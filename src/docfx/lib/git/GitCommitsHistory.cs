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
    internal class GitCommitsHistory
    {
        [JsonProperty("last_build_commit_id")]
        public string LastBuildCommitId { get; set; }

        [JsonProperty("commits")]
        public List<CommitsHistoryItem> Commits { get; set; } = new List<CommitsHistoryItem>();

        [JsonProperty("cross_repository_references")]
        public Dictionary<string, GitCommitsHistory> CrossRepositoryReferences { get; set; }
            = new Dictionary<string, GitCommitsHistory>();

        /// <summary>
        /// Get the dictionary recording commits time
        /// </summary>
        /// <returns>A Dictionary keyed with commit sha, valued with commit server time</returns>
        public Dictionary<string, DateTime> ToDictionary() => Commits.ToDictionary(c => c.Sha, c => c.BuiltAt);

        /// <summary>
        /// Create an instance of <see cref="GitCommitsHistory"/> from local cache
        /// </summary>
        /// <param name="path">the path of the cache file</param>
        public static GitCommitsHistory Create(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(File.Exists(path));

            try
            {
                var json = File.ReadAllText(path);
                return JsonUtility.Deserialize<GitCommitsHistory>(json);
            }
            catch (Exception ex)
            {
                throw Errors.InvalidGitCommitsHistory(path, ex).ToException(ex);
            }
        }
    }
}
