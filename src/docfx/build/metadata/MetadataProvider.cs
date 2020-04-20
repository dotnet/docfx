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
        private readonly JsonSchemaValidator[] _schemaValidators;
        private readonly JObject _globalMetadata;
        private readonly HashSet<string> _reservedMetadata;
        private readonly List<(Func<string, bool> glob, string key, JToken value)> _rules
            = new List<(Func<string, bool> glob, string key, JToken value)>();

        private readonly ConcurrentDictionary<FilePath, (List<Error> errors, UserMetadata metadata)> _metadataCache
                   = new ConcurrentDictionary<FilePath, (List<Error> errors, UserMetadata metadata)>();

        public JsonSchema[] MetadataSchemas { get; }

        public ICollection<string> HtmlMetaHidden { get; }

        public IReadOnlyDictionary<string, string> HtmlMetaNames { get; }

        public MetadataProvider(
            Config config, Input input, MicrosoftGraphAccessor microsoftGraphAccessor, FileResolver fileResolver, BuildScope buildScope)
        {
            _input = input;
            _globalMetadata = config.GlobalMetadata.ExtensionData;
            _buildScope = buildScope;

            MetadataSchemas = Array.ConvertAll(
                config.MetadataSchema,
                schema => JsonUtility.Deserialize<JsonSchema>(fileResolver.ReadString(schema), schema.Source?.File));

            _schemaValidators = Array.ConvertAll(
                MetadataSchemas,
                schema => new JsonSchemaValidator(schema, microsoftGraphAccessor));

            _reservedMetadata = JsonUtility.GetPropertyNames(typeof(SystemMetadata))
                .Concat(JsonUtility.GetPropertyNames(typeof(ConceptualModel)))
                .Concat(MetadataSchemas.SelectMany(schema => schema.Reserved))
                .Except(JsonUtility.GetPropertyNames(typeof(UserMetadata)))
                .ToHashSet();

            HtmlMetaHidden = MetadataSchemas.SelectMany(schema => schema.HtmlMetaHidden).ToHashSet();

            HtmlMetaNames = new Dictionary<string, string>(
                from schema in MetadataSchemas
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

        public List<Error> PostValidate()
        {
            var errors = new List<Error>();
            foreach (var validator in _schemaValidators)
            {
                errors.AddRange(validator.PostValidate());
            }

            return errors;
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

            foreach (var (key, value) in result)
            {
                if (value is null)
                {
                    continue;
                }
                if (_reservedMetadata.Contains(key))
                {
                    errors.Add(Errors.Metadata.AttributeReserved(JsonUtility.GetKeySourceInfo(value), key));
                }
                else if (!IsValidMetadataType(value))
                {
                    errors.Add(Errors.Metadata.InvalidMetadataType(JsonUtility.GetSourceInfo(value), key));
                }
            }

            var mime = _buildScope.GetMime(contentType, filePath);
            foreach (var schemaValidator in _schemaValidators)
            {
                // Only validate conceptual files
                if (contentType == ContentType.Page && mime == "Conceptual" && !result.ContainsKey("layout"))
                {
                    errors.AddRange(schemaValidator.Validate(result));
                }
            }

            var (validationErrors, metadata) = JsonUtility.ToObject<UserMetadata>(result);

            metadata.RawJObject = result;

            errors.AddRange(validationErrors);

            return (errors, metadata);
        }

        private static bool IsValidMetadataType(JToken token)
        {
            if (token is JObject)
            {
                return false;
            }

            if (token is JArray array && !array.All(item => item is JValue))
            {
                return false;
            }

            return true;
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
