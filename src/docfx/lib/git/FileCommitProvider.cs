// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using static Microsoft.Docs.Build.LibGit2;

namespace Microsoft.Docs.Build
{
    internal sealed class FileCommitProvider : IDisposable
    {
        private readonly Repository _repository;
        private readonly Lazy<GitCommitCache> _commitCache;

        // Commit history and a lookup table from commit hash to commit.
        // Use `long` to represent SHA2 git hashes for more efficient lookup and smaller size.
        private readonly ConcurrentDictionary<string, Lazy<(List<Commit>, Dictionary<long, Commit>)>> _commits;

        // Intern path strings by given each path segment a string ID. For faster string lookup.
        private readonly ConcurrentDictionary<string, int> _stringPool = new ConcurrentDictionary<string, int>();

        // A giant memory cache of git tree. Key is the `long` form of SHA2 tree hash, value is a string id to git SHA2 hash.
        private readonly ConcurrentDictionary<long, Dictionary<int, git_oid>> _trees
                   = new ConcurrentDictionary<long, Dictionary<int, git_oid>>();

        private int _nextStringId;
        private IntPtr _repo;

        public FileCommitProvider(Repository repository, string cacheFilePath)
        {
            Debug.Assert(repository != null);
            if (git_repository_open(out _repo, repository.Path) != 0)
            {
                throw new ArgumentException($"Invalid git repo {repository.Path}");
            }
            _repository = repository;
            _commitCache = new Lazy<GitCommitCache>(() => new GitCommitCache(cacheFilePath));
            _commits = new ConcurrentDictionary<string, Lazy<(List<Commit>, Dictionary<long, Commit>)>>();
        }

        public List<GitCommit> GetCommitHistory(string committish = null)
        {
            return GetCommitHistory("", committish);
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

        private List<GitCommit> GetCommitHistory(GitCommitCache.FileCommitCache commitCache, string file, string committish = null)
        {
            const int MaxParentBlob = 32;

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
                if (!TryRemoveCommit(commit, commitsToFollow, out var blob))
                {
                    continue;
                }

                // Lookup and use cached commit history ONLY if there are no other commits to follow
                if (commitsToFollow.Count == 0 && commitCache.TryGetCommits(commit.Sha.a, blob, out var commitIds))
                {
                    // Only update cache when the cached result has changed.
                    updateCache = result.Count != 0;

                    for (var i = 0; i < commitIds.Length; i++)
                    {
                        result.Add(commitsBySha[commitIds[i]]);
                    }
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
                    commitCache.SetCommits(headCommit.Sha.a, headBlob, result.Select(c => c.Sha.a).ToArray());
                }
            }

            return result.Select(c => c.GitCommit).ToList();
        }

        private static bool TryRemoveCommit(Commit commit, List<(Commit commit, long blob)> commitsToFollow, out long blob)
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

        private unsafe (List<Commit>, Dictionary<long, Commit>) LoadCommits(string committish = null)
        {
            if (string.IsNullOrEmpty(committish))
            {
                committish = _repository.Commit;
            }

            var commits = new List<Commit>();
            var commitsBySha = new Dictionary<long, Commit>();

            // walk commit list
            git_revwalk_new(out var walk, _repo);
            git_revwalk_sorting(walk, 1 << 0 | 1 << 1 /* GIT_SORT_TOPOLOGICAL | GIT_SORT_TIME */);

            if (git_revparse_single(out var headCommit, _repo, committish) != 0)
            {
                git_object_free(walk);
                throw Errors.CommittishNotFound(_repository.Path, committish).ToException();
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
                    throw Errors.GitCloneIncomplete(_repository.Path).ToException();
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

            if (committish.Equals(_repository.Commit))
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
