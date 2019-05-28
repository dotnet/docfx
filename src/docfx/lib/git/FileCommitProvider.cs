// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using static Microsoft.Docs.Build.LibGit2;

#pragma warning disable CA2002 // Do not lock on objects with weak identity

namespace Microsoft.Docs.Build
{
    internal sealed class FileCommitProvider : IDisposable
    {
        private const int MaxCommitCacheCountPerFile = 10;

        private readonly string _repoPath;
        private readonly string _cacheFilePath;

        // Commit history and a lookup table from commit hash to commit.
        // Use `long` to represent SHA2 git hashes for more efficient lookup and smaller size.
        private readonly ConcurrentDictionary<string, Lazy<(List<Commit>, Dictionary<long, Commit>)>> _commits;

        // Intern path strings by given each path segment a string ID. For faster string lookup.
        private readonly ConcurrentDictionary<string, int> _stringPool = new ConcurrentDictionary<string, int>();

        // A giant memory cache of git tree. Key is the `long` form of SHA2 tree hash, value is a string id to git SHA2 hash.
        private readonly ConcurrentDictionary<long, Dictionary<int, git_oid>> _trees
                   = new ConcurrentDictionary<long, Dictionary<int, git_oid>>();

        // Commit history LRU cache per file. Key is the file path relative to repository root.
        // Value is a dictionary of git commit history for a particular commit hash and file blob hash.
        // Only the last N = MaxCommitCacheCountPerFile commit histories are cached for a file, they are selected by least recently used order (lruOrder).
        private readonly Lazy<ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>> _commitCache;

        private int _nextLruOrder;
        private int _nextStringId;
        private bool _cacheUpdated;
        private IntPtr _repo;

        public FileCommitProvider(string repoPath, string cacheFilePath)
        {
            if (git_repository_open(out _repo, repoPath) != 0)
            {
                throw new ArgumentException($"Invalid git repo {repoPath}");
            }
            _repoPath = repoPath;
            _cacheFilePath = cacheFilePath;
            _commits = new ConcurrentDictionary<string, Lazy<(List<Commit>, Dictionary<long, Commit>)>>();
            _commitCache = new Lazy<ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>>(
                () => LoadCommitCache(_cacheFilePath));
        }

        public List<GitCommit> GetCommitHistory(
            string file,
            string committish)
        {
            Debug.Assert(!file.Contains('\\'));

            const int MaxParentBlob = 32;

            var commitCache = _commitCache.Value.GetOrAdd(file, _ => new Dictionary<(long, long), (long[], int)>());

            var (commits, commitsBySha) = _commits.GetOrAdd(
                committish ?? "",
                key => new Lazy<(List<Commit>, Dictionary<long, Commit>)>(() => LoadCommits(key))).Value;

            if (commits.Count <= 0)
            {
                return new List<GitCommit>();
            }

            var updateCache = true;
            var result = new List<Commit>();
            var parentBlobs = new long[MaxParentBlob];
            var pathSegments = Array.ConvertAll(file.Split('/'), GetStringId);

            var headCommit = commits[0];
            var headBlob = GetBlob(commits[0].Tree, pathSegments);
            var commitsToFollow = new List<(Commit commit, long blob)> { (headCommit, headBlob) };

            // `commits` is the commit history for the current branch,
            // the commit history for a file is always a subset of commit history of a branch with the same order.
            // Reusing a single branch commit history is a performance optimization.
            foreach (var commit in commits)
            {
                // Find and remove if this commit should be followed by the tree traversal.
                if (!TryRemoveCommit(commit, out var blob))
                {
                    continue;
                }

                // Lookup and use cached commit history ONLY if there are no other commits to follow
                if (commitsToFollow.Count == 0 && TryPopulateResultFromCache(commit, blob))
                {
                    break;
                }

                var singleParent = false;
                var parentCount = Math.Min(MaxParentBlob, commit.Parents.Length);

                for (var i = 0; i < parentCount; i++)
                {
                    parentBlobs[i] = GetBlob(commit.Parents[i].Tree, pathSegments);
                    if (parentBlobs[i] == blob)
                    {
                        // and it was TREESAME to one parent, follow only that parent.
                        // (Even if there are several TREESAME parents, follow only one of them.)
                        commitsToFollow.Add((commit.Parents[i], blob));
                        singleParent = true;
                        break;
                    }
                }

                if (!singleParent)
                {
                    // Otherwise, follow all parents.
                    for (var i = 0; i < parentCount; i++)
                    {
                        commitsToFollow.Add((commit.Parents[i], parentBlobs[i]));
                    }
                }

                if ((parentCount == 0 && blob != 0) || (parentCount > 0 && !singleParent))
                {
                    result.Add(commit);
                }
            }

            if (updateCache)
            {
                lock (commitCache)
                {
                    _cacheUpdated = true;
                    commitCache[(headCommit.Sha.a, headBlob)] = (result.Select(c => c.Sha.a).ToArray(), 0);
                }
            }

            return result.Select(c => c.GitCommit).ToList();

            bool TryRemoveCommit(Commit commit, out long blob)
            {
                blob = 0L;
                for (var i = 0; i < commitsToFollow.Count; i++)
                {
                    var commitToCheck = commitsToFollow[i];
                    if (commitToCheck.commit == commit)
                    {
                        blob = commitToCheck.blob;
                        commitsToFollow.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }

            bool TryPopulateResultFromCache(Commit commit, long blob)
            {
                lock (commitCache)
                {
                    if (commitCache.TryGetValue((commit.Sha.a, blob), out var cachedValue))
                    {
                        // Only update cache when the cached result has changed.
                        updateCache = result.Count != 0;

                        var (cachedCommitHistory, lruOrder) = cachedValue;
                        foreach (var cachedCommit in cachedCommitHistory)
                        {
                            result.Add(commitsBySha[cachedCommit]);
                        }
                        commitCache[(commit.Sha.a, blob)] = (cachedCommitHistory, _nextLruOrder--);
                        return true;
                    }
                    return false;
                }
            }
        }

        public void SaveCache()
        {
            SaveCacheCore();
        }

        public void Dispose()
        {
            var repo = Interlocked.Exchange(ref _repo, IntPtr.Zero);
            if (repo != IntPtr.Zero)
            {
                git_repository_free(_repo);
                GC.SuppressFinalize(this);
            }
        }

        ~FileCommitProvider()
        {
            Dispose();
        }

        private unsafe (List<Commit>, Dictionary<long, Commit>) LoadCommits(string committish = null)
        {
            if (string.IsNullOrEmpty(committish))
            {
                committish = "HEAD";
            }

            var commits = new List<Commit>();
            var commitsBySha = new Dictionary<long, Commit>();

            // walk commit list
            git_revwalk_new(out var walk, _repo);
            git_revwalk_sorting(walk, 1 << 0 | 1 << 1 /* GIT_SORT_TOPOLOGICAL | GIT_SORT_TIME */);

            if (git_revparse_single(out var headCommit, _repo, committish) != 0)
            {
                git_object_free(walk);
                throw Errors.CommittishNotFound(_repoPath, committish).ToException();
            }

            var lastCommitId = *git_object_id(headCommit);
            git_revwalk_push(walk, &lastCommitId);
            git_object_free(headCommit);

            while (true)
            {
                var error = git_revwalk_next(out var commitId, walk);
                if (error == -31 /* GIT_ITEROVER */)
                {
                    break;
                }

                // https://github.com/libgit2/libgit2sharp/issues/1351
                if (error != 0 /* GIT_ENOTFOUND */)
                {
                    git_revwalk_free(walk);

                    Log.Write($"Load git commit failed: {error} {lastCommitId}");
                    throw Errors.GitCloneIncomplete(_repoPath).ToException();
                }

                lastCommitId = commitId;
                git_object_lookup(out var commit, _repo, &commitId, 1 /* GIT_OBJ_COMMIT */);
                var author = git_commit_author(commit);
                var parentCount = git_commit_parentcount(commit);
                var parents = new git_oid[parentCount];
                for (var i = 0; i < parentCount; i++)
                {
                    parents[i] = *git_commit_parent_id(commit, i);
                }

                var item = new Commit
                {
                    Sha = commitId,
                    ParentShas = parents,
                    Tree = *git_commit_tree_id(commit),
                    GitCommit = new GitCommit
                    {
                        AuthorName = Marshal.PtrToStringUTF8(author->name),
                        AuthorEmail = Marshal.PtrToStringUTF8(author->email),
                        Sha = commitId.ToString(),
                        Time = new git_time { time = git_commit_time(commit), offset = git_commit_time_offset(commit) }.ToDateTimeOffset(),
                    },
                };
                commitsBySha.Add(commitId.a, item);
                commits.Add(item);
                git_object_free(commit);
            }
            git_revwalk_free(walk);

            // build parent indices
            Parallel.ForEach(commits, commit =>
            {
                commit.Parents = new Commit[commit.ParentShas.Length];
                for (var i = 0; i < commit.ParentShas.Length; i++)
                {
                    commit.Parents[i] = commitsBySha[commit.ParentShas[i].a];
                }
                commit.ParentShas = null;
            });

            if (committish.Equals("HEAD"))
            {
                Telemetry.TrackBuildCommitCount(commits.Count());
            }

            return (commits, commitsBySha);
        }

        private long GetBlob(git_oid treeId, int[] pathSegments)
        {
            var blob = treeId;

            for (var i = 0; i < pathSegments.Length; i++)
            {
                var files = _trees.GetOrAdd(blob.a, _ => LoadTree(blob));
                if (files is null || !files.TryGetValue(pathSegments[i], out blob))
                {
                    return default;
                }
            }

            return blob.a;
        }

        private unsafe Dictionary<int, git_oid> LoadTree(git_oid treeId)
        {
            if (git_object_lookup(out var tree, _repo, &treeId, 2 /* GIT_OBJ_TREE */) != 0)
            {
                return null;
            }

            var n = git_tree_entrycount(tree);
            var blobs = new Dictionary<int, git_oid>((int)n);

            for (var p = IntPtr.Zero; p != n; p = p + 1)
            {
                var entry = git_tree_entry_byindex(tree, p);
                var name = Marshal.PtrToStringUTF8(git_tree_entry_name(entry));

                blobs[GetStringId(name)] = *git_tree_entry_id(entry);
            }

            git_object_free(tree);

            return blobs;
        }

        private int GetStringId(string value)
        {
            return _stringPool.GetOrAdd(value, _ => Interlocked.Increment(ref _nextStringId));
        }

        private static ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>
            LoadCommitCache(string cacheFilePath)
        {
            Telemetry.TrackCacheTotalCount(TelemetryName.GitCommitCache);
            if (!File.Exists(cacheFilePath))
            {
                Telemetry.TrackCacheMissCount(TelemetryName.GitCommitCache);
                return new ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>();
            }

            Log.Write($"Using git commit history cache file: '{cacheFilePath}'");
            return ProcessUtility.ReadFile(cacheFilePath, stream =>
            {
                var result = new ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>();
                using (var reader = new BinaryReader(stream))
                {
                    var fileCount = reader.ReadInt32();
                    for (var fileIndex = 0; fileIndex < fileCount; fileIndex++)
                    {
                        var file = reader.ReadString();
                        var cacheCount = reader.ReadInt32();
                        var cachedCommits = result.GetOrAdd(file, _ => new Dictionary<(long, long), (long[], int)>());

                        for (var cacheIndex = 0; cacheIndex < cacheCount; cacheIndex++)
                        {
                            var commit = reader.ReadInt64();
                            var blob = reader.ReadInt64();
                            var commitCount = reader.ReadInt32();
                            var commitHistory = new long[commitCount];

                            for (var commitIndex = 0; commitIndex < commitCount; commitIndex++)
                            {
                                commitHistory[commitIndex] = reader.ReadInt64();
                            }
                            cachedCommits.Add((commit, blob), (commitHistory, cacheIndex + 1));
                        }
                    }
                }
                return result;
            });
        }

        private void SaveCacheCore()
        {
            if (!_cacheUpdated || string.IsNullOrEmpty(_cacheFilePath) || !_commitCache.IsValueCreated)
            {
                return;
            }

            PathUtility.CreateDirectoryFromFilePath(_cacheFilePath);

            ProcessUtility.WriteFile(_cacheFilePath, stream =>
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // Create a snapshot of commit cache to ensure count and items matches.
                    //
                    // There is a race condition in Linq ToList() method, use ConcurrentDictionary.ToArray() to create a snapshot
                    // https://stackoverflow.com/questions/11692389/getting-argument-exception-in-concurrent-dictionary-when-sorting-and-displaying
                    var commitCache = _commitCache.Value.ToArray();

                    writer.Write(commitCache.Length);
                    foreach (var (file, value) in commitCache)
                    {
                        lock (value)
                        {
                            writer.Write(file);
                            writer.Write(Math.Min(value.Count, MaxCommitCacheCountPerFile));

                            var lruValues = value.OrderBy(pair => pair.Value.lruOrder).Take(MaxCommitCacheCountPerFile);

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
            });
        }

        private class Commit
        {
            public git_oid Sha;

            public git_oid Tree;

            public git_oid[] ParentShas;

            public Commit[] Parents;

            public GitCommit GitCommit;
        }
    }
}
