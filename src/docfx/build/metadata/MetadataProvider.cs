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

        public (Error error, JObject metadata) GetMetadata(Document file, JObject yamlHeader = null)
        {
            Debug.Assert(file != null);

            var fileMetadata = new JObject();
            foreach (var (glob, key, value) in _rules)
            {
                if (glob(file.FilePath))
                {
                    fileMetadata[key] = value;
                }
            }

            var result = new JObject();
            result.Merge(_config.GlobalMetadata, JsonUtility.MergeSettings);
            result.Merge(fileMetadata, JsonUtility.MergeSettings);

            if (yamlHeader != null)
            {
                result.Merge(yamlHeader, JsonUtility.MergeSettings);
            }

            if (result.TryGetValue("redirect_url", StringComparison.OrdinalIgnoreCase, out _))
            {
                return (Errors.RedirectionInMetadata(), result);
            }

            return (null, result);
        }
    }
}
