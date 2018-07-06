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

        /// <summary>
        /// Create an instance of <see cref="GitCommitsTime"/> from local file
        /// </summary>
        /// <param name="path">the path of the file</param>
        public static GitCommitsTime Create(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(File.Exists(path));

            try
            {
                var json = File.ReadAllText(path);
                var (_, value) = JsonUtility.Deserialize<GitCommitsTime>(json);
                return value;
            }
            catch (Exception ex)
            {
                throw Errors.InvalidGitCommitsTime(path, ex).ToException(ex);
            }
        }
    }
}
