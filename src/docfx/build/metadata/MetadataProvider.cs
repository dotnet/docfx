// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class MetadataProvider
    {
        private readonly Input _input;
        private readonly bool _hasMonikerRangeFileMetadata;
        private readonly BuildScope _buildScope;
        private readonly JObject _globalMetadata;
        private readonly List<(Func<string, bool> glob, string key, JToken value)> _rules = new List<(Func<string, bool> glob, string key, JToken value)>();

        private readonly ConcurrentDictionary<FilePath, UserMetadata> _metadataCache = new ConcurrentDictionary<FilePath, UserMetadata>();

        public ICollection<string> HtmlMetaHidden { get; }

        public IReadOnlyDictionary<string, string> HtmlMetaNames { get; }

        public MetadataProvider(Config config, Input input, FileResolver fileResolver, BuildScope buildScope)
        {
            _input = input;
            _globalMetadata = config.GlobalMetadata.ExtensionData;
            _buildScope = buildScope;

            var metadataSchemas = Array.ConvertAll(
                config.MetadataSchema,
                schema => JsonUtility.DeserializeData<JsonSchema>(fileResolver.ReadString(schema), schema.Source?.File));

            HtmlMetaHidden = metadataSchemas.SelectMany(schema => schema.HtmlMetaHidden).ToHashSet();

            HtmlMetaNames = new Dictionary<string, string>(
                from schema in metadataSchemas
                from property in schema.Properties
                let htmlMetaName = property.Value.HtmlMetaName
                where !string.IsNullOrEmpty(htmlMetaName)
                select new KeyValuePair<string, string>(property.Key, htmlMetaName));

            _hasMonikerRangeFileMetadata = config.FileMetadata.ContainsKey("monikerRange");

            foreach (var (key, item) in config.FileMetadata)
            {
                foreach (var (glob, value) in item.Value)
                {
                    JsonUtility.SetKeySourceInfo(value, item.Source?.KeySourceInfo);
                    _rules.Add((GlobUtility.CreateGlobMatcher(glob), key, value));
                }
            }
        }

        public UserMetadata GetMetadata(ErrorBuilder errors, FilePath path)
        {
            var contentType = _buildScope.GetContentType(path);

            switch (contentType)
            {
                case ContentType.Unknown:
                case ContentType.Redirection when !_hasMonikerRangeFileMetadata:
                case ContentType.Resource when !_hasMonikerRangeFileMetadata:
                    return new UserMetadata();

                default:
                    return _metadataCache.GetOrAdd(path, _ => GetMetadataCore(errors, path, contentType));
            }
        }

        private UserMetadata GetMetadataCore(ErrorBuilder errors, FilePath filePath, ContentType contentType)
        {
            var result = new JObject();

            JsonUtility.SetSourceInfo(result, new SourceInfo(filePath, 1, 1));
            JsonUtility.Merge(result, _globalMetadata);

            var fileMetadata = new JObject();
            foreach (var (glob, key, value) in _rules)
            {
                if (glob(filePath.Path))
                {
                    fileMetadata.SetProperty(key, value);
                }
            }

            if (filePath.OriginalPath != null)
            {
                foreach (var (glob, key, value) in _rules)
                {
                    if (glob(filePath.OriginalPath.Path))
                    {
                        fileMetadata.SetProperty(key, value);
                    }
                }
            }
            JsonUtility.Merge(result, fileMetadata);

            if (contentType == ContentType.Page || contentType == ContentType.TableOfContents)
            {
                var yamlHeader = LoadYamlHeader(errors, filePath);
                if (yamlHeader.Count > 0)
                {
                    JsonUtility.Merge(result, yamlHeader);
                    JsonUtility.SetSourceInfo(result, JsonUtility.GetSourceInfo(yamlHeader));
                }
            }

            var metadata = JsonUtility.ToObject<UserMetadata>(errors, result);

            metadata.RawJObject = result;

            return metadata;
        }

        private JObject LoadYamlHeader(ErrorBuilder errors, FilePath file)
        {
            return file.Format switch
            {
                FileFormat.Markdown => ExtractYamlHeader.Extract(errors, _input.ReadText(file), file),
                FileFormat.Yaml => LoadSchemaDocumentYamlHeader(errors, _input.ReadYaml(errors, file), file),
                FileFormat.Json => LoadSchemaDocumentYamlHeader(errors, _input.ReadJson(errors, file), file),
                _ => new JObject(),
            };
        }

        private static JObject LoadSchemaDocumentYamlHeader(ErrorBuilder errors, JToken token, FilePath file)
        {
            var metadata = token is JObject tokenObj ? tokenObj["metadata"] : null;

            if (metadata != null)
            {
                if (metadata is JObject obj)
                {
                    return obj;
                }

                errors.Add(Errors.Yaml.YamlHeaderNotObject(isArray: metadata is JArray, file));
            }

            return new JObject();
        }
    }
}
