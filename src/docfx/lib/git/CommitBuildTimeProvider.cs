// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class CommitBuildTimeProvider
{
    private static readonly object s_lock = new();
    private static readonly DateTime s_now = DateTime.UtcNow;

    private readonly DateTime _buildTime;
    private readonly Repository _repo;
    private readonly Config _config;
    private readonly string _commitBuildTimePath;
    private IReadOnlyDictionary<string, DateTime> _buildTimeByCommit;

    public CommitBuildTimeProvider(Config config, Repository repo)
    {
        _repo = repo;
        _config = config;
        _commitBuildTimePath = AppData.BuildHistoryStatePath;
        _buildTime = config.BuildTime ?? s_now;

        _buildTimeByCommit = ReadLatestCacheIfAny();
    }

    public DateTime GetCommitBuildTime(string commitId)
        => _buildTimeByCommit.TryGetValue(commitId, out var time) ? time : _buildTime;

    public void Save()
    {
        if (!_config.UpdateCommitBuildTime || _buildTimeByCommit.ContainsKey(_repo.Commit))
        {
            return;
        }

        lock (s_lock)
        {
            using (PerfScope.Start($"Saving commit build time for {_repo.Path} {_repo.Commit}"))
            {
                _buildTimeByCommit = ReadLatestCacheIfAny();
                var commits = _buildTimeByCommit.Select(item => new CommitBuildTimeItem { Sha = item.Key, BuiltAt = item.Value }).ToList();

                // TODO: retrieve git log from `GitCommitProvider` since it should already be there.
                foreach (var diffCommit in GitUtility.GetCommits(_repo.Path, _repo.Commit))
                {
                    if (!_buildTimeByCommit.ContainsKey(diffCommit))
                    {
                        commits.Add(new CommitBuildTimeItem { Sha = diffCommit, BuiltAt = _buildTime });
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_commitBuildTimePath)) ?? ".");

                ProcessUtility.WriteJsonFile(_commitBuildTimePath, new CommitBuildTime { Commits = commits });
            }
        }
    }

    private IReadOnlyDictionary<string, DateTime> ReadLatestCacheIfAny()
    {
        var exists = File.Exists(_commitBuildTimePath);
        Log.Write($"{(exists ? "Using" : "Missing")} git commit build time cache file: '{_commitBuildTimePath}'");

        if (exists)
        {
            var commitBuildTime = ProcessUtility.ReadJsonFile<CommitBuildTime>(_commitBuildTimePath);
            return commitBuildTime.Commits.ToDictionary(item => item.Sha, item => item.BuiltAt);
        }
        else
        {
            return _buildTimeByCommit ?? new Dictionary<string, DateTime>();
        }
    }
}
