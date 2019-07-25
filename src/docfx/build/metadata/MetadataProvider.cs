// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class MetadataProvider
    {
        private readonly Cache _cache;
        private readonly JsonSchemaValidator _schemaValidator;
        private readonly JObject _globalMetadata;
        private readonly HashSet<string> _reservedMetadata;
        private readonly List<(Func<string, bool> glob, string key, JToken value)> _rules = new List<(Func<string, bool> glob, string key, JToken value)>();

        private readonly ConcurrentDictionary<Document, (List<Error> errors, InputMetadata metadata)> _metadataCache
                   = new ConcurrentDictionary<Document, (List<Error> errors, InputMetadata metadata)>();

        public MetadataProvider(Docset docset, Cache cache, MicrosoftGraphCache microsoftGraphCache)
        {
            _cache = cache;
            _schemaValidator = new JsonSchemaValidator(docset.MetadataSchema, microsoftGraphCache);
            _globalMetadata = docset.Config.GlobalMetadata;

            _reservedMetadata = JsonUtility.GetPropertyNames(typeof(OutputMetadata))
                .Concat(JsonUtility.GetPropertyNames(typeof(ConceptualModel)))
                .Concat(docset.MetadataSchema.Reserved)
                .Except(JsonUtility.GetPropertyNames(typeof(InputMetadata)))
                .ToHashSet();

            foreach (var (key, item) in docset.Config.FileMetadata)
            {
                foreach (var (glob, value) in item)
                {
                    _rules.Add((GlobUtility.CreateGlobMatcher(glob), key, value));
                }
            }
        }

        public (List<Error> errors, InputMetadata metadata) GetMetadata(Document file)
        {
            return _metadataCache.GetOrAdd(file, GetMetadataCore);
        }

        private (List<Error> errors, InputMetadata metadata) GetMetadataCore(Document file)
        {
            if (file.ContentType != ContentType.Page && file.ContentType != ContentType.TableOfContents)
            {
                return (new List<Error>(), new InputMetadata());
            }

            var result = new JObject();

            var (errors, yamlHeader) = LoadMetadata(file);
            JsonUtility.SetSourceInfo(result, JsonUtility.GetSourceInfo(yamlHeader));

            JsonUtility.Merge(result, _globalMetadata);

            var fileMetadata = new JObject();
            foreach (var (glob, key, value) in _rules)
            {
                if (glob(file.FilePath.Path))
                {
                    // Assign a JToken to a property erases line info, so clone here.
                    // See https://github.com/JamesNK/Newtonsoft.Json/issues/2055
                    fileMetadata[key] = JsonUtility.DeepClone(value);
                    JsonUtility.SetSourceInfo(fileMetadata.Property(key), JsonUtility.GetSourceInfo(value));
                }
            }
            JsonUtility.Merge(result, fileMetadata);
            JsonUtility.Merge(result, yamlHeader);

            foreach (var property in result.Properties())
            {
                if (_reservedMetadata.Contains(property.Name))
                {
                    errors.Add(Errors.AttributeReserved(JsonUtility.GetSourceInfo(property), property.Name));
                }
                else if (!IsValidMetadataType(property.Value))
                {
                    errors.Add(Errors.InvalidMetadataType(JsonUtility.GetSourceInfo(property.Value), property.Name));
                }
            }

            if (file.ContentType == ContentType.Page && string.IsNullOrEmpty(file.Mime))
            {
                errors.AddRange(_schemaValidator.Validate(result));
            }

            var (validationErrors, metadata) = JsonUtility.ToObject<InputMetadata>(result);

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
            if (file.FilePath.EndsWith(".md", PathUtility.PathComparison))
            {
                using (var reader = new StreamReader(file.ReadStream()))
                {
                    return ExtractYamlHeader.Extract(reader, file);
                }
            }

            if (file.FilePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                return LoadSchemaDocumentMetadata(_cache.LoadYamlFile(file), file);
            }

            if (file.FilePath.EndsWith(".json", PathUtility.PathComparison))
            {
                return LoadSchemaDocumentMetadata(_cache.LoadJsonFile(file), file);
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
