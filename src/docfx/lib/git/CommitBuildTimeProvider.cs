// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class CommitBuildTimeProvider
    {
        private readonly Repository _repo;
        private readonly string _commitBuildTimePath;
        private readonly IReadOnlyDictionary<string, DateTime> _buildTimeByCommit;

        public CommitBuildTimeProvider(Repository repo)
        {
            _repo = repo;
            _commitBuildTimePath = AppData.GetCommitBuildTimePath(repo.Remote, repo.Branch);

            var commitBuildTime = new CommitBuildTime();
            if (File.Exists(_commitBuildTimePath))
            {
                Log.Write($"Using git commit build time cache file: '{_commitBuildTimePath}'");
                commitBuildTime = JsonUtility.Deserialize<CommitBuildTime>(ProcessUtility.ReadFile(_commitBuildTimePath));
            }

            _buildTimeByCommit = commitBuildTime.Commits.ToDictionary(item => item.Sha, item => item.BuiltAt);
        }

        public bool TryGetCommitBuildTime(string commitId, out DateTime time)
        {
            return _buildTimeByCommit.TryGetValue(commitId, out time);
        }

        public void UpdateAndSaveCache()
        {
            if (_buildTimeByCommit.ContainsKey(_repo.Commit))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var commits = _buildTimeByCommit.Select(item => new CommitBuildTimeItem { Sha = item.Key, BuiltAt = item.Value }).ToList();

            // TODO: retrive git log from `FileCommitProvider` since it should already be there.
            foreach (var diffCommit in GitUtility.GetCommits(_repo.Path, _repo.Commit))
            {
                if (!_buildTimeByCommit.ContainsKey(diffCommit))
                {
                    commits.Add(new CommitBuildTimeItem { Sha = diffCommit, BuiltAt = now });
                }
            }

            PathUtility.CreateDirectoryFromFilePath(_commitBuildTimePath);
            File.WriteAllText(
                _commitBuildTimePath,
                JsonUtility.Serialize(new CommitBuildTime { Commits = commits }));
        }
    }
}
