// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using static Microsoft.Docs.Build.LibGit2;

namespace Microsoft.Docs.Build
{
    internal sealed class FileCommitProvider : IDisposable
    {
        private readonly string _repoPath;
        private readonly Lazy<GitCommitCache> _commitCache;

        // Intern path strings by given each path segment a string ID. For faster string lookup.
        private readonly ConcurrentDictionary<string, int> _stringPool = new ConcurrentDictionary<string, int>();

        // A giant memory cache of git tree. Key is the `long` form of SHA2 tree hash, value is a string id to git SHA2 hash.
        private readonly ConcurrentDictionary<long, Dictionary<int, git_oid>> _trees
                   = new ConcurrentDictionary<long, Dictionary<int, git_oid>>();

        // A cache of commit by commit id
        private readonly ConcurrentDictionary<long, NativeGitCommit> _commits = new ConcurrentDictionary<long, NativeGitCommit>();

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

        public List<GitCommit> GetCommitHistory(string file, string committish = null)
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

        private unsafe List<GitCommit> GetCommitHistory(GitCommitCache.FileCommitCache commitCache, string file, string committish = null)
        {
            if (git_revparse_single(out var pHead, _repo, committish ?? "HEAD") != 0)
            {
                throw Errors.CommittishNotFound(_repoPath, committish).ToException();
            }

            var updateCache = true;
            var result = new List<GitCommit>();
            var commitIds = new List<git_oid>();
            var parents = new (NativeGitCommit commit, long blob)[8];
            var pathSegments = Array.ConvertAll(file.Split('/'), GetStringId);

            var headCommit = GetCommit(git_commit_id(pHead));
            var headBlob = GetBlob(headCommit.Tree, pathSegments);

            var visitedNodes = new HashSet<long> { headCommit.Sha.a };

            var stack = new Stack<(NativeGitCommit commit, long blob)>();
            stack.Push((headCommit, headBlob));

            while (stack.TryPop(out var node))
            {
                var (commit, blob) = node;

                // Lookup and use cached commit history ONLY if there are no other commits to follow
                if (stack.Count == 0 && commitCache.TryGetCommits(commit.Sha.a, blob, out var cachedIds))
                {
                    // Only update cache when the cached result has changed.
                    updateCache = result.Count != 0;

                    for (var i = 0; i < cachedIds.Length; i++)
                    {
                        result.Add(GetCommit(cachedIds[i]).GitCommit);
                        commitIds.Add(cachedIds[i]);
                    }
                    break;
                }

                var singleParent = false;
                var parentCount = commit.Parents.Length;
                if (parents.Length < parentCount)
                {
                    Array.Resize(ref parents, parentCount);
                }

                for (var i = 0; i < parentCount; i++)
                {
                    var parent = GetCommit(commit.Parents[i]);
                    var parentBlob = GetBlob(parent.Tree, pathSegments);
                    parents[i] = (parent, parentBlob);

                    if (parentBlob == blob)
                    {
                        // and it was TREESAME to one parent, follow only that parent.
                        // (Even if there are several TREESAME parents, follow only one of them.)
                        if (visitedNodes.Add(parent.Sha.a))
                        {
                            stack.Push((parent, blob));
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
                        if (visitedNodes.Add(parents[i].commit.Sha.a))
                        {
                            stack.Push(parents[i]);
                        }
                    }
                }

                if ((parentCount == 0 && blob != 0) || (parentCount > 0 && !singleParent))
                {
                    result.Add(commit.GitCommit);
                    commitIds.Add(commit.Sha);
                }
            }

            // `git log` sorted commits by reverse chronological order
            result.Sort();

            if (updateCache)
            {
                lock (commitCache)
                {
                    commitCache.SetCommits(headCommit.Sha.a, headBlob, commitIds.ToArray());
                }
            }

            return result;
        }

        private unsafe NativeGitCommit GetCommit(git_oid commitId)
        {
            return GetCommit(&commitId);
        }

        private unsafe NativeGitCommit GetCommit(git_oid* commitId)
        {
            if (_commits.TryGetValue(commitId->a, out var existingCommit))
            {
                return existingCommit;
            }

            if (git_object_lookup(out var commit, _repo, commitId, 1 /* GIT_OBJ_COMMIT */) != 0)
            {
                throw Errors.CommittishNotFound(_repoPath, commitId->ToString()).ToException();
            }

            var author = git_commit_author(commit);
            var parentCount = git_commit_parentcount(commit);

            var parents = new git_oid[parentCount];
            for (var i = 0; i < parentCount; i++)
            {
                parents[i] = *git_commit_parent_id(commit, i);
            }

            var time = new git_time { time = git_commit_time(commit), offset = git_commit_time_offset(commit) }.ToDateTimeOffset();

            var result = new NativeGitCommit
            {
                Sha = *commitId,
                Tree = *git_commit_tree_id(commit),
                Parents = parents,
                GitCommit = new GitCommit
                {
                    AuthorName = Marshal.PtrToStringUTF8(author->name),
                    AuthorEmail = Marshal.PtrToStringUTF8(author->email),
                    Sha = commitId->ToString(),
                    Time = time,
                },
            };

            return _commits[commitId->a] = result;
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

            public git_oid[] Parents;

            public GitCommit GitCommit;
        }
    }
}
