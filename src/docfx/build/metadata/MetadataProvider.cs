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
        private readonly Config _config;
        private readonly List<(Func<string, bool> glob, string key, JToken value)> _rules = new List<(Func<string, bool> glob, string key, JToken value)>();
        private readonly List<string> _pageMetadataKeys = JsonUtility.GetPropertyNames(typeof(PageModel)).ToList();

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

        public (List<Error> errors, T metadata) GetMetadata<T>(Document file, JObject yamlHeader = null)
        {
            Debug.Assert(file != null);

            var errors = new List<Error>();
            var result = new JObject();

            JsonUtility.Merge(result, _config.GlobalMetadata);

            var fileMetadata = new JObject();
            foreach (var (glob, key, value) in _rules)
            {
                if (glob(file.FilePath))
                {
                    fileMetadata[key] = value;
                }
            }
            JsonUtility.Merge(result, fileMetadata);

            if (yamlHeader != null)
            {
                errors.AddRange(MetadataValidator.ValidateGlobalMetadata(yamlHeader));
                JsonUtility.Merge(result, yamlHeader);
            }

            // We are validating against the merged JObject so discard the validation result here.
            var (_, obj) = JsonUtility.ToObject<T>(result);
            return (errors, obj);
        }

        public JObject GetPageMetadata(FileMetadata fileMetadata)
        {
            var metadata = JsonUtility.ToJObject(fileMetadata ?? new FileMetadata());

            foreach (var key in _pageMetadataKeys)
            {
                metadata.Remove(key);
            }

            return metadata;
        }
    }
}
