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

            JsonUtility.Merge(result, _config.GlobalMetadata);

            var fileMetadata = new JObject();
            foreach (var (glob, key, value) in _rules)
            {
                if (glob(file.FilePath))
                {
                    fileMetadata[key] = JsonUtility.DeepClone(value);
                }
            }
            JsonUtility.Merge(result, fileMetadata);

            if (yamlHeader != null)
            {
                JsonUtility.Merge(result, yamlHeader);
            }

            var (errors, metadata) = JsonUtility.ToObject<T>(result);
            errors.AddRange(MetadataValidator.Validate(result));
            return (errors, metadata);
        }
    }
}
