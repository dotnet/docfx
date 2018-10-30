// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static Microsoft.Docs.Build.LibGit2;

#pragma warning disable CA2002 // Do not lock on objects with weak identity

namespace Microsoft.Docs.Build
{
    internal sealed class GitCommitProvider : IDisposable
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
        private readonly ConcurrentDictionary<long, Dictionary<int, GitOid>> _trees
                   = new ConcurrentDictionary<long, Dictionary<int, GitOid>>();

        // Commit history LRU cache per file. Key is the file path relative to repository root.
        // Value is a dictionary of git commit history for a particular commit hash and file blob hash.
        // Only the last N = MaxCommitCacheCountPerFile commit histories are cached for a file, they are selected by least recently used order (lruOrder).
        private readonly ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>> _commitCache;

        private int _nextLruOrder;
        private int _nextStringId;
        private bool _cacheUpdated;
        private IntPtr _repo;

        private GitCommitProvider(
            string repoPath,
            string cacheFilePath,
            ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>> commitCache)
        {
            _repoPath = repoPath;
            _cacheFilePath = cacheFilePath;
            _repo = GitUtility.OpenRepo(repoPath);
            _commits = new ConcurrentDictionary<string, Lazy<(List<Commit>, Dictionary<long, Commit>)>>();
            _commitCache = commitCache;
        }

        public static async Task<GitCommitProvider> Create(string repoPath, string cacheFilePath = null)
        {
            return new GitCommitProvider(repoPath, cacheFilePath, await LoadCommitCache(cacheFilePath));
        }

        public List<GitCommit> GetCommitHistory(string file, string branch = null)
        {
            Debug.Assert(!file.Contains('\\'));

            const int MaxParentBlob = 32;

            var (commits, commitsBySha) = _commits.GetOrAdd(
                branch ?? "",
                key => new Lazy<(List<Commit>, Dictionary<long, Commit>)>(() => LoadCommits(key))).Value;

            if (commits.Count <= 0)
            {
                return new List<GitCommit>();
            }

            var updateCache = true;
            var result = new List<Commit>();
            var parentBlobs = new long[MaxParentBlob];
            var pathSegments = Array.ConvertAll(file.Split('/'), GetStringId);
            var commitCache = _commitCache.GetOrAdd(file, _ => new Dictionary<(long, long), (long[], int)>());

            var headCommit = commits[0];
            var headBlob = GetBlob(commits[0].Tree, pathSegments);
            var commitsToFollow = new List<(Commit commit, long blob)> { (headCommit, headBlob) };

            // `commits` is the commit history for the current branch,
            // the commit history for a file is always a subset of commit history of a branch with the same order.
            // Reusing a single branch commit history is a performance optimization.
            foreach (var commit in commits)
            {
                // Find and remove if this commit should be followed by the tree traversal.
                var found = false;
                var blob = 0L;
                for (var i = 0; i < commitsToFollow.Count; i++)
                {
                    var commitToCheck = commitsToFollow[i];
                    if (commitToCheck.commit == commit)
                    {
                        blob = commitToCheck.blob;
                        commitsToFollow.RemoveAt(i);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    continue;
                }

                // Lookup and use cached commit history ONLY if there are no other commits to follow
                if (commitsToFollow.Count == 0)
                {
                    lock (commitCache)
                    {
                        if (commitCache.TryGetValue((commit.Sha.A, blob), out var cachedValue))
                        {
                            updateCache = result.Count != 0;

                            var (cachedCommitHistory, lruOrder) = cachedValue;
                            foreach (var cachedCommit in cachedCommitHistory)
                            {
                                result.Add(commitsBySha[cachedCommit]);
                            }
                            commitCache[(commit.Sha.A, blob)] = (cachedCommitHistory, _nextLruOrder--);
                            break;
                        }
                    }
                }

                var singleParent = false;
                var parentCount = Math.Min(MaxParentBlob, commit.Parents.Length);
                var add = parentCount == 0 && blob != 0;

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
                        add = true;
                        commitsToFollow.Add((commit.Parents[i], parentBlobs[i]));
                    }
                }

                if (add)
                {
                    result.Add(commit);
                }
            }

            if (updateCache)
            {
                lock (commitCache)
                {
                    _cacheUpdated = true;
                    commitCache.Add((headCommit.Sha.A, headBlob), (result.Select(c => c.Sha.A).ToArray(), 0));
                }
            }

            return result.Select(c => c.GitCommit).ToList();
        }

        public Task SaveCache()
        {
            if (!_cacheUpdated || string.IsNullOrEmpty(_cacheFilePath))
            {
                return Task.CompletedTask;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_cacheFilePath));

            return ProcessUtility.RunInsideMutex(_cacheFilePath, () =>
            {
                using (var stream = File.Create(_cacheFilePath))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(_commitCache.Count);
                    foreach (var (file, value) in _commitCache)
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
                return Task.CompletedTask;
            });
        }

        public void Dispose()
        {
            var repo = Interlocked.Exchange(ref _repo, IntPtr.Zero);
            if (repo != IntPtr.Zero)
            {
                GitRepositoryFree(_repo);
            }
        }

        private unsafe (List<Commit>, Dictionary<long, Commit>) LoadCommits(string branchName = null)
        {
            var commits = new List<Commit>();
            var commitsBySha = new Dictionary<long, Commit>();

            // walk commit list
            GitRevwalkNew(out var walk, _repo);
            GitRevwalkSorting(walk, 1 << 0 | 1 << 1 /* GIT_SORT_TOPOLOGICAL | GIT_SORT_TIME */);

            if (string.IsNullOrEmpty(branchName))
            {
                GitRevwalkPushHead(walk);
            }
            else
            {
                fixed (byte* pLocaleBranchName = ToUtf8Native($"{branchName}"))
                fixed (byte* pRemoteBranchName = ToUtf8Native($"origin/{branchName}"))
                {
                    if (GitBranchLookup(out var refBranch, _repo, pLocaleBranchName, 1 /*locale branch*/) == 0 ||
                        GitBranchLookup(out refBranch, _repo, pRemoteBranchName, 2 /*remote branch*/) == 0)
                    {
                        var commit = GitReferenceTarget(refBranch);
                        GitRevwalkPush(walk, commit);
                    }
                    else
                    {
                        throw new ArgumentException($"{branchName} can't not be resolved");
                    }
                }
            }

            while (true)
            {
                var error = GitRevwalkNext(out var commitId, walk);
                if (error == -31 /* GIT_ITEROVER */)
                    break;

                // https://github.com/libgit2/libgit2sharp/issues/1351
                if (error == -3 /* GIT_ENOTFOUND */)
                    throw Errors.GitShadowClone(_repoPath).ToException();

                if (error != 0)
                    throw new InvalidOperationException($"Unknown error calling git_revwalk_next: {error}");

                GitObjectLookup(out var commit, _repo, &commitId, GitObjectType.Commit);
                var author = GitCommitAuthor(commit);
                var committer = GitCommitCommitter(commit);
                var parentCount = GitCommitParentcount(commit);
                var parents = new GitOid[parentCount];
                for (var i = 0; i < parentCount; i++)
                {
                    parents[i] = *GitCommitParentId(commit, i);
                }

                var item = new Commit
                {
                    Sha = commitId,
                    ParentShas = parents,
                    Tree = *GitCommitTreeId(commit),
                    GitCommit = new GitCommit
                    {
                        AuthorName = FromUtf8Native(author->Name),
                        AuthorEmail = FromUtf8Native(author->Email),
                        Sha = commitId.ToString(),
                        Time = ToDateTimeOffset(GitCommitTime(commit), GitCommitTimeOffset(commit)),
                    },
                };
                commitsBySha.Add(commitId.A, item);
                commits.Add(item);
                GitObjectFree(commit);
            }
            GitRevwalkFree(walk);

            // build parent indices
            Parallel.ForEach(commits, commit =>
            {
                commit.Parents = new Commit[commit.ParentShas.Length];
                for (var i = 0; i < commit.ParentShas.Length; i++)
                {
                    commit.Parents[i] = commitsBySha[commit.ParentShas[i].A];
                }
                commit.ParentShas = null;
            });

            return (commits, commitsBySha);
        }

        private long GetBlob(GitOid treeId, int[] pathSegments)
        {
            var blob = treeId;

            for (var i = 0; i < pathSegments.Length; i++)
            {
                var files = _trees.GetOrAdd(blob.A, _ => LoadTree(blob));
                if (files == null || !files.TryGetValue(pathSegments[i], out blob))
                {
                    return default;
                }
            }

            return blob.A;
        }

        private unsafe Dictionary<int, GitOid> LoadTree(GitOid treeId)
        {
            if (GitObjectLookup(out var tree, _repo, &treeId, GitObjectType.Tree) != 0)
            {
                return null;
            }

            var n = GitTreeEntrycount(tree);
            var blobs = new Dictionary<int, GitOid>((int)n);

            for (var p = IntPtr.Zero; p != n; p = p + 1)
            {
                var entry = GitTreeEntryByindex(tree, p);
                var name = FromUtf8Native(GitTreeEntryName(entry));

                blobs[GetStringId(name)] = *GitTreeEntryId(entry);
            }

            GitObjectFree(tree);

            return blobs;
        }

        private int GetStringId(string value)
        {
            return _stringPool.GetOrAdd(value, _ => Interlocked.Increment(ref _nextStringId));
        }

        private static async Task<ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>>
            LoadCommitCache(string cacheFilePath)
        {
            var result = new ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>();
            if (string.IsNullOrEmpty(cacheFilePath) || !File.Exists(cacheFilePath))
            {
                return result;
            }

            await ProcessUtility.RunInsideMutex(cacheFilePath, () =>
            {
                using (var stream = File.OpenRead(cacheFilePath))
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
                return Task.CompletedTask;
            });

            return result;
        }

        private class Commit
        {
            public GitOid Sha;

            public GitOid Tree;

            public GitOid[] ParentShas;

            public Commit[] Parents;

            public GitCommit GitCommit;

            public override string ToString() => Sha.ToString();
        }
    }
}
