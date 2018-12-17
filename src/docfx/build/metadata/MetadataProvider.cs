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
        private static readonly HashSet<string> _reservedNames = GetReservedMetadata();

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

            var invalidNames = new List<string>();
            foreach (var (key, _) in result)
            {
                if (_reservedNames.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    invalidNames.Add(key);
                }
            }

            if (invalidNames.Count > 0)
            {
                return (Errors.ReservedMetadata(invalidNames), result);
            }

            return (null, result);
        }

        private static HashSet<string> GetReservedMetadata()
        {
            var blackList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var outputProperties = typeof(PageModel).GetProperties();
            foreach (var propertyInfo in outputProperties)
            {
                blackList.Add(propertyInfo.Name);
                blackList.Add(ToUnderscoreCase(propertyInfo.Name));
            }

            var inputProperties = typeof(FileMetadata).GetProperties();
            foreach (var propertyInfo in inputProperties)
            {
                blackList.Remove(propertyInfo.Name);
                blackList.Remove(ToUnderscoreCase(propertyInfo.Name));
            }

            return blackList;
        }

        private static string ToUnderscoreCase(string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLowerInvariant();
        }
    }
}
