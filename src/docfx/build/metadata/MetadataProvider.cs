// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class MetadataProvider
    {
        private readonly JObject _globalMetadata;
        private readonly HashSet<string> _reservedMetadata;
        private readonly List<(Func<string, bool> glob, string key, JToken value)> _rules = new List<(Func<string, bool> glob, string key, JToken value)>();

        public MetadataProvider(Docset docset)
        {
            _globalMetadata = docset.Config.GlobalMetadata;

            _reservedMetadata = JsonUtility.GetPropertyNames(typeof(OutputModel))
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

        public (List<Error> errors, T metadata) GetInputMetadata<T>(Document file, JObject yamlHeader = null) where T : InputMetadata
        {
            Debug.Assert(file != null);

            var result = new JObject();
            if (yamlHeader != null)
            {
                JsonUtility.SetSourceInfo(result, JsonUtility.GetSourceInfo(yamlHeader));
            }

            JsonUtility.Merge(result, _globalMetadata);

            var fileMetadata = new JObject();
            foreach (var (glob, key, value) in _rules)
            {
                if (glob(file.FilePath))
                {
                    // Assign a JToken to a property erases line info, so clone here.
                    // See https://github.com/JamesNK/Newtonsoft.Json/issues/2055
                    fileMetadata[key] = JsonUtility.DeepClone(value);
                    JsonUtility.SetSourceInfo(fileMetadata.Property(key), JsonUtility.GetSourceInfo(value));
                }
            }
            JsonUtility.Merge(result, fileMetadata);

            if (yamlHeader != null)
            {
                JsonUtility.Merge(result, yamlHeader);
            }

            var (errors, metadata) = JsonUtility.ToObject<T>(result);

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

            errors.AddRange(JsonSchemaValidation.Validate(file.Docset.MetadataSchema, result));

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
    }
}
