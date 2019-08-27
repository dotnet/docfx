// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using static Microsoft.Docs.Build.LibGit2;

namespace Microsoft.Docs.Build
{
    internal sealed class FileCommitProvider : IDisposable
    {
        private readonly string _repoPath;
        private readonly Lazy<GitCommitCache> _commitCache;

        // Commit history and a lookup table from commit hash to commit.
        // Use `long` to represent SHA2 git hashes for more efficient lookup and smaller size.
        private readonly ConcurrentDictionary<long, NativeGitCommit> _commits = new ConcurrentDictionary<long, NativeGitCommit>();

        // A giant memory cache of git tree. Key is the `long` form of SHA2 tree hash, value is a string id to git SHA2 hash.
        private readonly ConcurrentDictionary<long, Dictionary<int, git_oid>> _trees
                   = new ConcurrentDictionary<long, Dictionary<int, git_oid>>();

        // Intern path strings by given each path segment a string ID. For faster string lookup.
        private readonly ConcurrentDictionary<string, int> _stringPool = new ConcurrentDictionary<string, int>();

        // Reduce allocation for GetCommitHistory using an object pool.
        private readonly ConcurrentBag<HashSet<long>> _closeNodesPool = new ConcurrentBag<HashSet<long>>();

        private int _nextStringId;
        private IntPtr _repo;

        public FileCommitProvider(string repoPath, string cacheFilePath)
        {
            if (git_repository_open(out _repo, repoPath) != 0)
            {
                throw new ArgumentException($"Invalid git repo {repoPath}");
            }

            _repoPath = repoPath;
            _commitCache = new Lazy<GitCommitCache>(() => new GitCommitCache(cacheFilePath));
        }

        public GitCommit[] GetCommitHistory(string file, string committish = null)
        {
            Debug.Assert(!file.Contains('\\'));

            var commitCache = _commitCache.Value.ForFile(file);

            lock (commitCache)
            {
                return GetCommitHistory(commitCache, file, committish);
            }
        }

        public void Save()
        {
            if (_commitCache.IsValueCreated)
            {
                _commitCache.Value.Save();
            }
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

        private unsafe GitCommit[] GetCommitHistory(GitCommitCache.FileCommitCache commitCache, string file, string committish = null)
        {
            if (git_revparse_single(out var pHead, _repo, committish ?? "HEAD") != 0)
            {
                throw Errors.CommittishNotFound(_repoPath, committish).ToException();
            }

            var updateCache = true;
            var pathSegments = Array.ConvertAll(file.Split('/'), GetStringId);
            var headCommit = GetCommit(*git_commit_id(pHead));
            var headBlob = GetBlob(headCommit.Tree, pathSegments);

            var commits = new List<NativeGitCommit>();
            var parents = new (NativeGitCommit commit, long)[8];
            var openNodes = new Stack<(NativeGitCommit, long)>();
            var closeNodes = _closeNodesPool.TryTake(out var aCloseNodes) ? aCloseNodes : new HashSet<long>(1024);

            closeNodes.Add(headCommit.Sha.a);
            openNodes.Push((headCommit, headBlob));

            while (openNodes.TryPop(out var node))
            {
                var (commit, blob) = node;

                // Lookup and use cached commit history ONLY if there are no other commits to follow
                if (openNodes.Count == 0 && commitCache.TryGetCommits(commit.Sha.a, blob, out var commitIds))
                {
                    // Only update cache when the cached result has changed.
                    updateCache = commits.Count != 0;

                    for (var i = 0; i < commitIds.Length; i++)
                    {
                        commits.Add(GetCommit(commitIds[i]));
                    }
                    break;
                }

                var singleParent = false;
                var parentCount = commit.ParentIds.Length;
                if (parents.Length < parentCount)
                {
                    Array.Resize(ref parents, parentCount);
                }

                for (var i = 0; i < parentCount; i++)
                {
                    // Build up the commit graph as we traverse the commits
                    var parent = commit.Parents[i] ?? (commit.Parents[i] = GetCommit(commit.ParentIds[i]));
                    var parentBlob = GetBlob(parent.Tree, pathSegments);
                    parents[i] = (parent, parentBlob);

                    if (parentBlob == blob)
                    {
                        // and it was TREESAME to one parent, follow only that parent.
                        // (Even if there are several TREESAME parents, follow only one of them.)
                        if (closeNodes.Add(parent.Sha.a))
                        {
                            openNodes.Push((parent, blob));
                        }
                        singleParent = true;
                        break;
                    }
                }

                if (!singleParent)
                {
                    // Otherwise, follow all parents.
                    for (var i = 0; i < parentCount; i++)
                    {
                        if (closeNodes.Add(parents[i].commit.Sha.a))
                        {
                            openNodes.Push(parents[i]);
                        }
                    }
                }

                if ((parentCount == 0 && blob != 0) || (parentCount > 0 && !singleParent))
                {
                    commits.Add(commit);
                }
            }

            if (updateCache)
            {
                lock (commitCache)
                {
                    commitCache.SetCommits(headCommit.Sha.a, headBlob, commits.Select(c => c.Sha).ToArray());
                }
            }

            var result = commits.Select(c => c.GitCommit).ToArray();

            // `git log` sorted commits by reverse chronological order
            Array.Sort(result);

            closeNodes.Clear();
            _closeNodesPool.Add(closeNodes);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe NativeGitCommit GetCommit(in git_oid commitId)
        {
            return _commits.GetOrAdd(commitId.a, GetCommitCore, commitId);
        }

        private unsafe NativeGitCommit GetCommitCore(long unused, git_oid commitId)
        {
            if (git_object_lookup(out var commit, _repo, &commitId, 1 /* GIT_OBJ_COMMIT */) != 0)
            {
                throw Errors.CommittishNotFound(_repoPath, commitId.ToString()).ToException();
            }

            var author = git_commit_author(commit);
            var parentCount = git_commit_parentcount(commit);

            var parentIds = new git_oid[parentCount];
            for (var i = 0; i < parentCount; i++)
            {
                parentIds[i] = *git_commit_parent_id(commit, i);
            }

            var time = new git_time { time = git_commit_time(commit), offset = git_commit_time_offset(commit) }.ToDateTimeOffset();

            var result = new NativeGitCommit
            {
                Sha = commitId,
                Tree = *git_commit_tree_id(commit),
                ParentIds = parentIds,
                Parents = new NativeGitCommit[parentCount],
                GitCommit = new GitCommit
                {
                    AuthorName = Marshal.PtrToStringUTF8(author->name),
                    AuthorEmail = Marshal.PtrToStringUTF8(author->email),
                    Sha = commitId.ToString(),
                    Time = time,
                },
            };

            return result;
        }

        private long GetBlob(git_oid blob, int[] pathSegments)
        {
            for (var i = 0; i < pathSegments.Length; i++)
            {
                var files = _trees.GetOrAdd(blob.a, LoadTree, blob);
                if (files is null || !files.TryGetValue(pathSegments[i], out blob))
                {
                    return default;
                }
            }

            return blob.a;
        }

        private unsafe Dictionary<int, git_oid> LoadTree(long unused, git_oid treeId)
        {
            if (git_object_lookup(out var tree, _repo, &treeId, 2 /* GIT_OBJ_TREE */) != 0)
            {
                return null;
            }

            var n = git_tree_entrycount(tree);
            var blobs = new Dictionary<int, git_oid>((int)n);

            for (var p = IntPtr.Zero; p != n; p += 1)
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

        private class NativeGitCommit
        {
            public git_oid Sha;

            public git_oid Tree;

            public git_oid[] ParentIds;

            public NativeGitCommit[] Parents;

            public GitCommit GitCommit;
        }
    }
}
