// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class MetadataProvider
    {
        private readonly Config _config;
        private readonly List<(Func<string, bool> glob, string key, JToken value)> _rules = new List<(Func<string, bool> glob, string key, JToken value)>();

        public MetadataProvider(Config config)
        {
            _config = config;

            foreach (var (key, item) in config.FileMetadata)
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

            JsonUtility.Merge(result, _config.GlobalMetadata);

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

            errors.AddRange(MetadataValidator.Validate(result));
            errors.AddRange(JsonSchemaValidation.Validate(file.Docset.MetadataSchema, result));

            return (errors, metadata);
        }
    }
}
