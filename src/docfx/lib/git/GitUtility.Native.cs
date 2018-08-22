// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.Docs.Build
{
    internal static partial class GitUtility
    {
        /// <summary>
        /// Retrieve git repo information.
        /// </summary>
        public static unsafe (string remote, string branch, string commit) GetRepoInfo(string repoPath)
        {
            var (remote, branch, commit) = default((string, string, string));
            var pRepo = OpenRepo(repoPath);

            // TODO: marshal strings
            fixed (byte* pRemoteName = NativeMethods.ToUtf8Native("origin"))
            {
                if (NativeMethods.GitRemoteLookup(out var pRemote, pRepo, pRemoteName) == 0)
                {
                    remote = NativeMethods.FromUtf8Native(NativeMethods.GitRemoteUrl(pRemote));
                    NativeMethods.GitRemoteFree(pRemote);
                }
            }

            if (NativeMethods.GitRepositoryHead(out var pHead, pRepo) == 0)
            {
                commit = NativeMethods.GitReferenceTarget(pHead)->ToString();
                if (NativeMethods.GitBranchName(out var pName, pHead) == 0)
                {
                    branch = NativeMethods.FromUtf8Native(pName);
                }
                NativeMethods.GitReferenceFree(pHead);
            }

            NativeMethods.GitRepositoryFree(pRepo);

            return (remote, branch, commit);
        }

        /// <summary>
        /// Bulk get git commits for a list of files in a repo.
        /// </summary>
        public static unsafe List<GitCommit>[] GetCommits(string repoPath, List<string> files)
        {
            var (trees, commits) = default((ConcurrentDictionary<long, Dictionary<string, long>>, List<Commit>));
            var (paths, lookup) = CreatePathForReferenceEqualsLookup(files);

            var repo = OpenRepo(repoPath);
            var repoName = Path.GetFileName(repoPath);

            using (Progress.Start($"Loading git commits for '{repoPath}'"))
            {
                commits = LoadCommits(repoPath, repo);
                trees = LoadTrees(repo, commits, lookup, Progress.Update);
            }

            var (done, total, result) = (0, paths.Length, new List<GitCommit>[paths.Length]);

            using (Progress.Start($"Computing git commits for '{repoPath}'"))
            {
                Parallel.For(0, paths.Length, i =>
                {
                    result[i] = GetCommitsByPath(paths[i], trees, commits);
                    Progress.Update(Interlocked.Increment(ref done), total);
                });
            }

            NativeMethods.GitRepositoryFree(repo);
            return result;
        }

        private static (PathString[] paths, HashSet<string> lookup) CreatePathForReferenceEqualsLookup(List<string> files)
        {
            var lookup = new HashSet<string>(files.Count * 2);
            var paths = new PathString[files.Count];

            for (var i = 0; i < files.Count; i++)
            {
                var segments = files[i].Split('/');
                var refEqualsSegments = new string[segments.Length];

                for (var n = 0; n < segments.Length; n++)
                {
                    var segment = segments[n];
                    if (!lookup.TryGetValue(segment, out var refEqualsSegment))
                    {
                        lookup.Add(refEqualsSegment = segment);
                    }
                    refEqualsSegments[n] = refEqualsSegment;
                }

                paths[i] = new PathString { Segments = refEqualsSegments };
            }

            return (paths, lookup);
        }

        private static unsafe IntPtr OpenRepo(string path)
        {
            fixed (byte* pRepoPath = NativeMethods.ToUtf8Native(path))
            {
                if (NativeMethods.GitRepositoryOpen(out var repo, pRepoPath) != 0)
                {
                    throw new ArgumentException($"Invalid git repo {path}");
                }

                return repo;
            }
        }

        private static unsafe List<Commit> LoadCommits(string repoPath, IntPtr repo)
        {
            var commits = new List<Commit>();
            var commitToIndex = new Dictionary<NativeMethods.GitOid, int>();

            // walk commit list
            NativeMethods.GitRevwalkNew(out var walk, repo);
            NativeMethods.GitRevwalkSorting(walk, 1 << 0 | 1 << 1 /* GIT_SORT_TOPOLOGICAL | GIT_SORT_TIME */);
            NativeMethods.GitRevwalkPushHead(walk);

            while (true)
            {
                var error = NativeMethods.GitRevwalkNext(out var commitId, walk);
                if (error == -31 /* GIT_ITEROVER */)
                    break;

                // https://github.com/libgit2/libgit2sharp/issues/1351
                if (error == -3 /* GIT_ENOTFOUND */)
                    throw Errors.GitShadowClone(repoPath).ToException();

                if (error != 0)
                    throw new InvalidOperationException($"Unknown error calling git_revwalk_next: {error}");

                NativeMethods.GitObjectLookup(out var commit, repo, &commitId, NativeMethods.GitObjectType.Commit);
                var author = NativeMethods.GitCommitAuthor(commit);
                var committer = NativeMethods.GitCommitCommitter(commit);
                var parentCount = NativeMethods.GitCommitParentcount(commit);
                var parents = new NativeMethods.GitOid[parentCount];
                for (var i = 0; i < parentCount; i++)
                {
                    parents[i] = *NativeMethods.GitCommitParentId(commit, i);
                }
                commitToIndex.Add(commitId, commits.Count);
                commits.Add(new Commit
                {
                    Sha = commitId,
                    ParentShas = parents,
                    Tree = *NativeMethods.GitCommitTreeId(commit),
                    GitCommit = new GitCommit
                    {
                        AuthorName = NativeMethods.FromUtf8Native(author->Name),
                        AuthorEmail = NativeMethods.FromUtf8Native(author->Email),
                        Sha = commitId.ToString(),
                        Time = NativeMethods.ToDateTimeOffset(NativeMethods.GitCommitTime(commit), NativeMethods.GitCommitTimeOffset(commit)),
                    },
                });
                NativeMethods.GitObjectFree(commit);
            }
            NativeMethods.GitRevwalkFree(walk);

            // build parent indices
            Parallel.ForEach(commits, commit =>
            {
                commit.Parents = new Commit[commit.ParentShas.Length];
                for (var i = 0; i < commit.ParentShas.Length; i++)
                {
                    commit.Parents[i] = commits[commitToIndex[commit.ParentShas[i]]];
                }
                commit.ParentShas = null;
            });

            return commits;
        }

        private static unsafe ConcurrentDictionary<long, Dictionary<string, long>> LoadTrees(
            IntPtr repo, List<Commit> commits, HashSet<string> lookup, Action<int, int> progress)
        {
            // Reduce memory footprint by using `long` over `git_oid`
            // azure-docs-pr has 483947 distinct blobs, their first 8 bytes are also distinct.
            var done = 0;
            var total = commits.Count;
            var trees = new ConcurrentDictionary<long, Dictionary<string, long>>();

            Parallel.ForEach(commits, commit =>
            {
                var tree = commit.Tree;
                WalkTree(&tree);
                progress?.Invoke(Interlocked.Increment(ref done), total);
            });

            void WalkTree(NativeMethods.GitOid* treeId)
            {
                var blobs = new Dictionary<string, long>(RefComparer.Instance);
                if (!trees.TryAdd(treeId->A, blobs))
                {
                    return;
                }

                NativeMethods.GitObjectLookup(out var tree, repo, treeId, NativeMethods.GitObjectType.Tree);

                var n = NativeMethods.GitTreeEntrycount(tree);

                for (var p = IntPtr.Zero; p != n; p = p + 1)
                {
                    var entry = NativeMethods.GitTreeEntryByindex(tree, p);
                    var name = NativeMethods.FromUtf8Native(NativeMethods.GitTreeEntryName(entry));

                    if (lookup.TryGetValue(name, out var segment))
                    {
                        var blob = NativeMethods.GitTreeEntryId(entry);

                        blobs[segment] = blob->A;

                        if (NativeMethods.GitTreeEntryType(entry) == 2 /* GIT_OBJ_TREE */)
                        {
                            WalkTree(blob);
                        }
                    }
                }

                NativeMethods.GitObjectFree(tree);
            }

            return trees;
        }

        private static long GetBlob(ConcurrentDictionary<long, Dictionary<string, long>> trees, long tree, PathString file)
        {
            var blob = tree;

            for (var i = 0; i < file.Segments.Length; i++)
            {
                if (!trees.TryGetValue(blob, out var files) ||
                    !files.TryGetValue(file.Segments[i], out blob))
                {
                    return default;
                }
            }

            return blob;
        }

        private static unsafe List<GitCommit> GetCommitsByPath(
            PathString file, ConcurrentDictionary<long, Dictionary<string, long>> trees, List<Commit> commits)
        {
            const int MaxParentBlob = 32;

            var contributors = new List<GitCommit>();
            if (commits.Count <= 0)
                return contributors;

            var commitsToFollow = new List<(Commit commit, long blob)> { (commits[0], GetBlob(trees, commits[0].Tree.A, file)) };
            var parentBlobs = stackalloc long[MaxParentBlob];

            foreach (var commit in commits)
            {
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

                var singleParent = false;
                var parentCount = Math.Min(MaxParentBlob, commit.Parents.Length);
                var add = parentCount == 0 && blob != 0;

                for (var i = 0; i < parentCount; i++)
                {
                    parentBlobs[i] = GetBlob(trees, commit.Parents[i].Tree.A, file);
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
                    contributors.Add(commit.GitCommit);
                }
            }

            return contributors;
        }

        private class Commit
        {
            public NativeMethods.GitOid Sha { get; set; }

            public NativeMethods.GitOid Tree { get; set; }

            public NativeMethods.GitOid[] ParentShas { get; set; }

            public Commit[] Parents { get; set; }

            public GitCommit GitCommit { get; set; }

            public override string ToString() => Sha.ToString();
        }

        private struct PathString
        {
            public string[] Segments;

            public override string ToString() => string.Join('/', Segments);
        }

        private class RefComparer : EqualityComparer<string>
        {
            public static readonly RefComparer Instance = new RefComparer();

            public override bool Equals(string x, string y) => ReferenceEquals(x, y);

            public override int GetHashCode(string obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
