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
        private readonly DocumentProvider _documentProvider;
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
            Docset docset, Input input, MicrosoftGraphCache microsoftGraphCache, RestoreFileMap restoreFileMap, DocumentProvider documentProvider)
        {
            _input = input;
            _documentProvider = documentProvider;
            _globalMetadata = docset.Config.GlobalMetadata.ExtensionData;

            MetadataSchemas = Array.ConvertAll(
                docset.Config.MetadataSchema,
                schema => JsonUtility.Deserialize<JsonSchema>(
                    restoreFileMap.ReadString(schema), schema.Source.File));

            _schemaValidators = Array.ConvertAll(
                MetadataSchemas,
                schema => new JsonSchemaValidator(schema, microsoftGraphCache));

            _reservedMetadata = JsonUtility.GetPropertyNames(typeof(SystemMetadata))
                .Concat(JsonUtility.GetPropertyNames(typeof(ConceptualModel)))
                .Concat(MetadataSchemas.SelectMany(schema => schema.Reserved))
                .Except(JsonUtility.GetPropertyNames(typeof(UserMetadata)))
                .ToHashSet();

            HtmlMetaHidden = MetadataSchemas.SelectMany(schema => schema.HtmlMetaHidden).ToHashSet();

            HtmlMetaNames = MetadataSchemas.SelectMany(
                schema => schema.Properties.Where(prop => !string.IsNullOrEmpty(prop.Value.HtmlMetaName)))
                    .ToDictionary(prop => prop.Key, prop => prop.Value.HtmlMetaName);

            foreach (var (key, item) in docset.Config.FileMetadata)
            {
                foreach (var (glob, value) in item.Value)
                {
                    JsonUtility.SetKeySourceInfo(value, item.Source?.KeySourceInfo);
                    _rules.Add((GlobUtility.CreateGlobMatcher(glob), key, value));
                }
            }
        }

        public (List<Error> errors, UserMetadata metadata) GetMetadata(FilePath file)
        {
            return _metadataCache.GetOrAdd(file, GetMetadataCore);
        }

        private (List<Error> errors, UserMetadata metadata) GetMetadataCore(FilePath path)
        {
            var result = new JObject();
            var errors = new List<Error>();
            var yamlHeader = new JObject();

            var file = _documentProvider.GetDocument(path);

            if (file.ContentType == ContentType.Page || file.ContentType == ContentType.TableOfContents)
            {
                (errors, yamlHeader) = LoadMetadata(file);
                JsonUtility.SetSourceInfo(result, JsonUtility.GetSourceInfo(yamlHeader));
            }

            JsonUtility.Merge(result, _globalMetadata);

            var fileMetadata = new JObject();
            foreach (var (glob, key, value) in _rules)
            {
                if (glob(file.FilePath.Path))
                {
                    // Assign a JToken to a property erases line info, so clone here.
                    // See https://github.com/JamesNK/Newtonsoft.Json/issues/2055
                    fileMetadata[key] = JsonUtility.DeepClone(value);
                }
            }
            JsonUtility.Merge(result, fileMetadata);
            JsonUtility.Merge(result, yamlHeader);

            foreach (var (key, value) in result)
            {
                if (_reservedMetadata.Contains(key))
                {
                    errors.Add(Errors.AttributeReserved(JsonUtility.GetKeySourceInfo(value), key));
                }
                else if (!IsValidMetadataType(value))
                {
                    errors.Add(Errors.InvalidMetadataType(JsonUtility.GetSourceInfo(value), key));
                }
            }

            foreach (var schemaValidator in _schemaValidators)
            {
                // Only validate conceptual files
                if (file.ContentType == ContentType.Page && string.IsNullOrEmpty(file.Mime) && !result.ContainsKey("layout"))
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

        private (List<Error> errors, JObject metadata) LoadMetadata(Document file)
        {
            if (file.FilePath.EndsWith(".md"))
            {
                using (var reader = _input.ReadText(file.FilePath))
                {
                    return ExtractYamlHeader.Extract(reader, file);
                }
            }

            if (file.FilePath.EndsWith(".yml"))
            {
                return LoadSchemaDocumentMetadata(_input.ReadYaml(file.FilePath), file);
            }

            if (file.FilePath.EndsWith(".json"))
            {
                return LoadSchemaDocumentMetadata(_input.ReadJson(file.FilePath), file);
            }

            return (new List<Error>(), new JObject());
        }

        private static (List<Error> errors, JObject metadata) LoadSchemaDocumentMetadata((List<Error>, JToken) document, Document file)
        {
            var (errors, token) = document;
            var metadata = token is JObject tokenObj ? tokenObj["metadata"] : null;

            if (metadata != null)
            {
                if (metadata is JObject obj)
                {
                    return (errors, obj);
                }

                errors.Add(Errors.YamlHeaderNotObject(isArray: metadata is JArray, file.FilePath));
            }

            return (errors, new JObject());
        }
    }
}
