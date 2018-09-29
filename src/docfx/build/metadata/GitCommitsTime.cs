// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class GitCommitsTime
    {
        [JsonProperty("commits")]
        public List<CommitsTimeItem> Commits { get; set; } = new List<CommitsTimeItem>();

        /// <summary>
        /// Get the dictionary recording commits time
        /// </summary>
        /// <returns>A Dictionary keyed with commit sha, valued with commit server time</returns>
        public Dictionary<string, DateTime> ToDictionary() => Commits.ToDictionary(c => c.Sha, c => c.BuiltAt);
    }
}
