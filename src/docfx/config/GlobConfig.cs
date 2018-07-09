// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class GlobConfig<T>
    {
        /// <summary>
        /// Gets the include patterns of files.
        /// </summary>
        public string[] Include;

        /// <summary>
        /// Gets the exclude patterns of files.
        /// </summary>
        public string[] Exclude;

        /// <summary>
        /// Gets the value to apply to files.
        /// </summary>
        public T Value;

        /// <summary>
        /// Gets whether the value of <see cref="Include"/> and <see cref="Exclude"/> is glob pattern.
        /// </summary>
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IsGlob;

        public GlobConfig(string[] include, string[] exclude, T value, bool isGlob = true)
        {
            Debug.Assert(value != null);

            Include = include ?? Array.Empty<string>();
            Exclude = exclude ?? Array.Empty<string>();
            Value = value;
            IsGlob = isGlob;
        }

        public bool Match(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            if (Exclude.Any(e => MatchItem(filePath, e)))
                return false;
            if (Include.Any(i => MatchItem(filePath, i)))
                return true;

            return false;
        }

        private bool MatchItem(string filePath, string pattern)
        {
            if (pattern == null)
                return false;

            if (IsGlob)
            {
                // TODO: optimize this
                var options = PathUtility.IsCaseSensitive ? GlobMatcher.DefaultCaseSensitiveOptions : GlobMatcher.DefaultOptions;
                var glob = new GlobMatcher(pattern, options);
                return glob.Match(filePath);
            }
            else
            {
                return pattern.EndsWith('/') ?
                    filePath.StartsWith(pattern, PathUtility.PathComparison) :
                    filePath.Equals(pattern, PathUtility.PathComparison);
            }
        }
    }
}
