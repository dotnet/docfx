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
        private readonly BuildScope _buildScope;
        private readonly JObject _globalMetadata;
        private readonly List<(Func<string, bool> glob, string key, JToken value)> _rules = new List<(Func<string, bool>, string, JToken)>();
        private readonly List<(Func<string, bool> glob, string key, JToken value)> _monikerRules = new List<(Func<string, bool>, string, JToken)>();

        private readonly ConcurrentDictionary<FilePath, UserMetadata> _metadataCache = new ConcurrentDictionary<FilePath, UserMetadata>();

        public ICollection<string> HtmlMetaHidden { get; }

        public IReadOnlyDictionary<string, string> HtmlMetaNames { get; }

        public MetadataProvider(Config config, Input input, BuildScope buildScope, JsonSchemaLoader jsonSchemaLoader)
        {
            _input = input;
            _globalMetadata = config.GlobalMetadata.ExtensionData;
            _buildScope = buildScope;

            var metadataSchemas = Array.ConvertAll(config.MetadataSchema, jsonSchemaLoader.LoadSchema);

            HtmlMetaHidden = metadataSchemas.SelectMany(schema => schema.HtmlMetaHidden).ToHashSet();

            HtmlMetaNames = new Dictionary<string, string>(
                from schema in metadataSchemas
                from property in schema.Properties
                let htmlMetaName = property.Value.HtmlMetaName
                where !string.IsNullOrEmpty(htmlMetaName)
                select new KeyValuePair<string, string>(property.Key, htmlMetaName));

            foreach (var (key, item) in config.FileMetadata)
            {
                foreach (var (glob, value) in item.Value)
                {
                    JsonUtility.SetSourceInfo(value, JsonUtility.GetSourceInfo(value)?.WithKeySourceInfo(item.Source?.KeySourceInfo));
                    var matcher = GlobUtility.CreateGlobMatcher(glob);
                    _rules.Add((matcher, key, value));

                    if (key.Contains("moniker", StringComparison.OrdinalIgnoreCase))
                    {
                        _monikerRules.Add((matcher, key, value));
                    }
                }
            }
        }

        public UserMetadata GetMetadata(ErrorBuilder errors, FilePath file)
        {
            return _metadataCache.GetOrAdd(file, _ => GetMetadataCore(errors, file));
        }

        private UserMetadata GetMetadataCore(ErrorBuilder errors, FilePath file)
        {
            var result = new JObject();
            JsonUtility.SetSourceInfo(result, new SourceInfo(file, 1, 1));

            // We only care about moniker related metadata for redirections and resources
            var contentType = _buildScope.GetContentType(file);
            var hasYamlHeader = contentType == ContentType.Page || contentType == ContentType.TableOfContents;
            if (hasYamlHeader)
            {
                JsonUtility.Merge(result, _globalMetadata);
            }

            var fileMetadata = new JObject();
            var rules = hasYamlHeader ? _rules : _monikerRules;
            if (rules.Count > 0)
            {
                foreach (var (glob, key, value) in rules)
                {
                    if (glob(file.Path))
                    {
                        fileMetadata.SetProperty(key, value);
                    }
                }
                JsonUtility.Merge(result, fileMetadata);
            }

            if (hasYamlHeader)
            {
                var yamlHeader = LoadYamlHeader(errors, file);
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
