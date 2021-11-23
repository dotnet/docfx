// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build;

internal class GitCommitCache
{
    private const int MaxCommitCacheCountPerFile = 20;

    private readonly string _cacheFilePath;

    // Commit history LRU cache per file (or empty for whole repo). Key is the file path relative to repository root.
    // Value is a dictionary of git commit history for a particular commit hash and file blob hash.
    // Only the last N = MaxCommitCacheCountPerFile commit histories are cached for a file, they are selected by least recently used order (lruOrder).
    private readonly ConcurrentDictionary<string, FileCommitCache> _commitCache;

    private bool _cacheUpdated;

    public GitCommitCache(string cacheFilePath)
    {
        _cacheFilePath = cacheFilePath;
        _commitCache = Load();
    }

    public FileCommitCache ForFile(string file)
    {
        return _commitCache.GetOrAdd(file, _ => new FileCommitCache(this));
    }

    public void Save()
    {
        if (!_cacheUpdated)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_cacheFilePath)) ?? ".");

        ProcessUtility.WriteFile(_cacheFilePath, stream =>
        {
            using var writer = new BinaryWriter(stream);

            // Create a snapshot of commit cache to ensure count and items matches.
            //
            // There is a race condition in Linq ToList() method, use ConcurrentDictionary.ToArray() to create a snapshot
            // https://stackoverflow.com/questions/11692389/getting-argument-exception-in-concurrent-dictionary-when-sorting-and-displaying
            var commitCache = _commitCache.ToArray();

            writer.Write(commitCache.Length);
            foreach (var (file, cache) in commitCache)
            {
                writer.Write(file);
                lock (cache)
                {
                    cache.Save(writer);
                }
            }
        });
    }

    private ConcurrentDictionary<string, FileCommitCache> Load()
    {
        var commitCache = new ConcurrentDictionary<string, FileCommitCache>();
        if (!File.Exists(_cacheFilePath))
        {
            return commitCache;
        }

        Log.Write($"Using git commit history cache file: '{_cacheFilePath}'");
        ProcessUtility.ReadFile(_cacheFilePath, stream =>
        {
            using var reader = new BinaryReader(stream);
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var file = reader.ReadString();
                var cache = commitCache.GetOrAdd(file, _ => new FileCommitCache(this));

                lock (cache)
                {
                    cache.Load(reader);
                }
            }
        });

        return commitCache;
    }

    public class FileCommitCache
    {
        private readonly GitCommitCache _parent;
        private readonly Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)> _commits
                   = new();

        private int _nextLruOrder;

        internal FileCommitCache(GitCommitCache parent) => _parent = parent;

        public bool TryGetCommits(long sha, long blob, [NotNullWhen(true)] out long[]? commits)
        {
            if (_commits.TryGetValue((sha, blob), out var value))
            {
                (commits, _) = value;
                _commits[(sha, blob)] = (commits, _nextLruOrder--);
                return true;
            }
            commits = null;
            return false;
        }

        public void SetCommits(long sha, long blob, long[] commits)
        {
            _parent._cacheUpdated = true;
            _commits[(sha, blob)] = (commits, 0);
        }

        public void Load(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var commit = reader.ReadInt64();
                var blob = reader.ReadInt64();
                var commitCount = reader.ReadInt32();
                var commitHistory = new long[commitCount];

                for (var commitIndex = 0; commitIndex < commitCount; commitIndex++)
                {
                    commitHistory[commitIndex] = reader.ReadInt64();
                }
                _commits.Add((commit, blob), (commitHistory, i + 1));
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Math.Min(_commits.Count, MaxCommitCacheCountPerFile));

            var lruValues = _commits.OrderBy(pair => pair.Value.lruOrder).Take(MaxCommitCacheCountPerFile);

            foreach (var ((commit, blob), (commitHistory, _)) in lruValues)
            {
                writer.Write(commit);
                writer.Write(blob);
                writer.Write(commitHistory.Length);

                foreach (var sha in commitHistory)
                {
                    writer.Write(sha);
                }
            }
        }
    }
}
