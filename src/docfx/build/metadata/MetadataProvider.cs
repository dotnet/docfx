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
        private readonly List<(Func<string, bool> glob, string key, JToken value)> _rules
            = new List<(Func<string, bool> glob, string key, JToken value)>();

        private readonly ConcurrentDictionary<FilePath, (List<Error> errors, UserMetadata metadata)> _metadataCache
                   = new ConcurrentDictionary<FilePath, (List<Error> errors, UserMetadata metadata)>();

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

        public (List<Error> errors, UserMetadata metadata) GetMetadata(FilePath path)
        {
            var contentType = _buildScope.GetContentType(path);

            switch (contentType)
            {
                case ContentType.Unknown:
                case ContentType.Redirection when !_hasMonikerRangeFileMetadata:
                case ContentType.Resource when !_hasMonikerRangeFileMetadata:
                    return (new List<Error>(), new UserMetadata());

                default:
                    return _metadataCache.GetOrAdd(path, _ => GetMetadataCore(path, contentType));
            }
        }

        private (List<Error> errors, UserMetadata metadata) GetMetadataCore(FilePath filePath, ContentType contentType)
        {
            var result = new JObject();
            var errors = new List<Error>();

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
            JsonUtility.Merge(result, fileMetadata);

            if (contentType == ContentType.Page || contentType == ContentType.TableOfContents)
            {
                var (yamlHeaderErrors, yamlHeader) = LoadYamlHeader(filePath);
                errors.AddRange(yamlHeaderErrors);

                if (yamlHeader.Count > 0)
                {
                    JsonUtility.Merge(result, yamlHeader);
                    JsonUtility.SetSourceInfo(result, JsonUtility.GetSourceInfo(yamlHeader));
                }
            }

            var (validationErrors, metadata) = JsonUtility.ToObject<UserMetadata>(result);

            metadata.RawJObject = result;

            errors.AddRange(validationErrors);

            return (errors, metadata);
        }

        private (List<Error> errors, JObject yamlHeader) LoadYamlHeader(FilePath file)
        {
            switch (file.Format)
            {
                case FileFormat.Markdown:
                    using (var reader = _input.ReadText(file))
                    {
                        return ExtractYamlHeader.Extract(reader, file);
                    }
                case FileFormat.Yaml:
                    return LoadSchemaDocumentYamlHeader(_input.ReadYaml(file), file);
                case FileFormat.Json:
                    return LoadSchemaDocumentYamlHeader(_input.ReadJson(file), file);
                default:
                    return (new List<Error>(), new JObject());
            }
        }

        private static (List<Error> errors, JObject metadata) LoadSchemaDocumentYamlHeader((List<Error>, JToken) document, FilePath file)
        {
            var (errors, token) = document;
            var metadata = token is JObject tokenObj ? tokenObj["metadata"] : null;

            if (metadata != null)
            {
                if (metadata is JObject obj)
                {
                    return (errors, obj);
                }

                errors.Add(Errors.Yaml.YamlHeaderNotObject(isArray: metadata is JArray, file));
            }

            return (errors, new JObject());
        }
    }
}
