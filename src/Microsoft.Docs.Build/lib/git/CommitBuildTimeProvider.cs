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
        private readonly DateTime _buildTime;
        private readonly Repository _repo;
        private readonly Config _config;
        private readonly string _commitBuildTimePath;
        private readonly IReadOnlyDictionary<string, DateTime> _buildTimeByCommit;

        public CommitBuildTimeProvider(Config config, Repository repo)
        {
            _repo = repo;
            _config = config;
            _commitBuildTimePath = AppData.GetCommitBuildTimePath(repo.Remote, repo.Branch!);
            _buildTime = config.BuildTime ?? DateTime.UtcNow;

            var exists = File.Exists(_commitBuildTimePath);
            Log.Write($"{(exists ? "Using" : "Missing")} git commit build time cache file: '{_commitBuildTimePath}'");

            var commitBuildTime = exists
                ? ProcessUtility.ReadJsonFile<CommitBuildTime>(_commitBuildTimePath)
                : new CommitBuildTime();

            _buildTimeByCommit = commitBuildTime.Commits.ToDictionary(item => item.Sha, item => item.BuiltAt);
        }

        public DateTime GetCommitBuildTime(string commitId)
            => _buildTimeByCommit.TryGetValue(commitId, out var time) ? time : _buildTime;

        public void Save()
        {
            if (!_config.UpdateCommitBuildTime || _buildTimeByCommit.ContainsKey(_repo.Commit))
            {
                return;
            }

            using (PerfScope.Start($"Saving commit build time for {_repo.Commit}"))
            {
                var commits = _buildTimeByCommit.Select(item => new CommitBuildTimeItem { Sha = item.Key, BuiltAt = item.Value }).ToList();

                // TODO: retrieve git log from `FileCommitProvider` since it should already be there.
                foreach (var diffCommit in GitUtility.GetCommits(_repo.Path, _repo.Commit))
                {
                    if (!_buildTimeByCommit.ContainsKey(diffCommit))
                    {
                        commits.Add(new CommitBuildTimeItem { Sha = diffCommit, BuiltAt = _buildTime });
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_commitBuildTimePath)));

                ProcessUtility.WriteFile(
                    _commitBuildTimePath,
                    JsonUtility.Serialize(new CommitBuildTime { Commits = commits }));
            }
        }
    }
}
