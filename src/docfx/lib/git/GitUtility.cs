// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Docs.Git;

using FileMap = System.Collections.Generic.Dictionary<string, (string file, string parent)>;
using TreeMap = System.Collections.Concurrent.ConcurrentDictionary<long, System.Collections.Generic.Dictionary<string, long>>;

namespace Microsoft.Docs
{
    /// <summary>
    /// Provide git operations
    /// </summary>
    public static class GitUtility
    {
        private static readonly char[] s_newline = new[] { '\r', '\n' };

        /// <summary>
        /// Find git repo directory
        /// </summary>
        /// <param name="path">The git repo entry point</param>
        /// <returns>The git repo root path</returns>
        public static string FindRepo(string path)
        {
            Debug.Assert(!PathUtility.FolderPathHasInvalidChars(path));

            var repo = path;
            while (!string.IsNullOrEmpty(repo))
            {
                var gitPath = Path.Combine(repo, ".git");
                if (Directory.Exists(gitPath) || File.Exists(gitPath) /* submodule */)
                {
                    return repo;
                }
                repo = Path.GetDirectoryName(repo);
            }
            return repo;
        }

        /// <summary>
        /// Get git commits group by files
        /// </summary>
        /// <param name="repoPath">The git repo root path</param>
        /// <param name="files">The collection of git repo files</param>
        /// <param name="progress">The processing progress</param>
        /// <returns>A collection of git commits</returns>
        public static unsafe List<GitCommit>[] GetCommits(string repoPath, List<string> files, Action<int, int> progress = null)
        {
            Debug.Assert(files.All(file => !PathUtility.FilePathHasInvalidChars(file)));

            var pathToParent = BuildPathToParentPath(files);
            var pathToParentByRef = pathToParent.ToDictionary(p => p.Key, p => p.Value, RefComparer.Instance);
            var repo = OpenRepo(repoPath);
            var commits = LoadCommits(repo);
            var trees = LoadTrees(repo, commits, pathToParent, progress);

            var (done, total, result) = (0, files.Count, new List<GitCommit>[files.Count]);
            Parallel.For(0, files.Count, i =>
            {
                result[i] = GetCommitsByPath(files[i], trees, pathToParentByRef, commits);
                progress?.Invoke(Interlocked.Increment(ref done), total);
            });
            NativeMethods.GitRepositoryFree(repo);
            return result;
        }

        /// <summary>
        /// Init git repository
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <returns>Task status</returns>
        public static async Task Init(string cwd)
        {
            if (!Directory.Exists(cwd))
            {
                Directory.CreateDirectory(cwd);
            }

            await ExecuteNonQuery(cwd, $"init");
            await ExecuteNonQuery(cwd, @"commit -m ""Init Commit"" --allow-empty");
        }

        /// <summary>
        /// Clone git repository from remote to local
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="remote">The remote url</param>
        /// <param name="path">The path to clone</param>
        /// <returns>Task status</returns>
        public static Task Clone(string cwd, string remote, string path)
            => ExecuteNonQuery(cwd, $"clone {remote} {path.Replace("\\", "/", StringComparison.Ordinal)}", TimeSpan.FromMinutes(20));

        /// <summary>
        /// Fetch update from remote
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <returns>Task status</returns>
        public static Task Fetch(string cwd)
            => ExecuteNonQuery(cwd, $"fetch", TimeSpan.FromMinutes(3));

        /// <summary>
        /// Pull update from remote
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="remote">The remote name, default is origin</param>
        /// <returns>Task status</returns>
        public static Task Pull(string cwd, string remote = null)
            => ExecuteNonQuery(cwd, $"pull {remote ?? string.Empty}", TimeSpan.FromMinutes(3));

        /// <summary>
        /// Checkout repo to specified branch
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="create">Create this branch if not exists or not</param>
        /// <param name="branch">The branch name, default is master</param>
        /// <returns>Task status</returns>
        public static Task Checkout(string cwd, bool create, string branch = null)
            => ExecuteNonQuery(cwd, $"checkout {(create ? "-b" : "")} {branch ?? "master"}", TimeSpan.FromMinutes(5));

        /// <summary>
        /// Reset current repo to remote
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="branch">The branch name</param>
        /// <returns>Task status</returns>
        public static Task Reset(string cwd, string branch)
            => ExecuteNonQuery(cwd, $"reset --hard origin/{branch}", TimeSpan.FromMinutes(10));

        /// <summary>
        /// Retrieve git head version
        /// </summary>
        /// <param name="cwd">The working directory</param>
        /// <returns>The git head version</returns>
        public static Task<string> HeadRevision(string cwd)
           => ExecuteQuery(cwd, "rev-parse HEAD");

        /// <summary>
        /// Get commits per file
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="file">The file path</param>
        /// <param name="count">The commit count you want to retrieve</param>
        /// <returns>A collection of git commit info</returns>
        public static Task<IReadOnlyList<GitCommit>> GetCommits(string cwd, string file = null, int count = -1)
        {
            string formatter = "%H|%cI|%an|%ae|%cn|%ce";
            var argumentsBuilder = new StringBuilder();
            argumentsBuilder.Append($@"--no-pager log --format=""{formatter}""");
            if (count > 0)
            {
                argumentsBuilder.Append($" -{count}");
            }

            if (!string.IsNullOrEmpty(file))
            {
                argumentsBuilder.Append($@" -- ""{file}""");
            }

            return ExecuteQuery(cwd, argumentsBuilder.ToString(), ParseListCommitOutPut);
        }

        private static IReadOnlyList<GitCommit> ParseListCommitOutPut(string lines)
            => (from line in lines.Split(s_newline, StringSplitOptions.RemoveEmptyEntries)
                let parts = line.Split('|')
                select new GitCommit { Sha = parts[0], Time = DateTimeOffset.Parse(parts[1], null), AuthorName = parts[2], AuthorEmail = parts[3] }).ToList();

        private static FileMap BuildPathToParentPath(List<string> files)
        {
            var res = new HashSet<string>(files.Count + 128);
            foreach (var file in files)
            {
                for (var i = file.Length - 1; i >= 0; i--)
                {
                    if (file[i] == '/')
                    {
                        res.Add(file.Substring(0, i + 1));
                    }
                }
                res.Add(file);
            }
            return res.ToDictionary(file => file, file =>
            {
                var i = file.Length - 2;
                while (i >= 0 && file[i] != '/')
                {
                    i--;
                }

                return (file, i >= 0 && res.TryGetValue(file.Substring(0, i + 1), out var parent) ? parent : null);
            });
        }

        private static Task ExecuteNonQuery(string cwd, string commandLineArgs, TimeSpan? timeout = null)
            => Execute(cwd, commandLineArgs, timeout, x => x);

        private static Task<T> ExecuteQuery<T>(string cwd, string commandLineArgs, Func<string, T> parser, TimeSpan? timeout = null)
            => Execute(cwd, commandLineArgs, timeout, parser);

        private static Task<string> ExecuteQuery(string cwd, string commandLineArgs, TimeSpan? timeout = null)
            => Execute(cwd, commandLineArgs, timeout, x => x);

        private static async Task<T> Execute<T>(string cwd, string commandLineArgs, TimeSpan? timeout, Func<string, T> parser)
        {
            // todo: check git exist or not
            var response = await ProcessUtility.Execute("git", commandLineArgs, cwd, timeout);
            return parser(response);
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

        private static unsafe List<Commit> LoadCommits(IntPtr repo)
        {
            var commits = new List<Commit>();
            var commitToIndex = new Dictionary<NativeMethods.GitOid, int>();

            // walk commit list
            NativeMethods.GitRevwalkNew(out var walk, repo);
            NativeMethods.GitRevwalkSorting(walk, 1 << 0 | 1 << 1 /* GIT_SORT_TOPOLOGICAL | GIT_SORT_TIME */);
            NativeMethods.GitRevwalkPushHead(walk);

            while (NativeMethods.GitRevwalkNext(out var commitId, walk) == 0)
            {
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

        private static unsafe TreeMap LoadTrees(IntPtr repo, List<Commit> commits, FileMap pathToParent, Action<int, int> progress)
        {
            // Reduce memory footprint by using `long` over `git_oid`.
            // azure-docs-pr has 483947 distinct blobs, their first 8 bytes are also distinct.
            var trees = new TreeMap();
            var done = 0;
            var total = commits.Count;

            Parallel.ForEach(commits, commit =>
            {
                var tree = commit.Tree;
                var blobs = new Dictionary<string, long>(RefComparer.Instance);
                if (trees.TryAdd(tree.A, blobs))
                {
                    WalkTree(&tree, "", blobs);
                }
                progress?.Invoke(Interlocked.Increment(ref done), total);
            });

            return trees;

            void WalkTree(NativeMethods.GitOid* treeId, string path, Dictionary<string, long> blobs)
            {
                NativeMethods.GitObjectLookup(out var tree, repo, treeId, NativeMethods.GitObjectType.Tree);
                var n = NativeMethods.GitTreeEntrycount(tree);
                for (var p = IntPtr.Zero; p != n; p = p + 1)
                {
                    var entry = NativeMethods.GitTreeEntryByindex(tree, p);
                    var name = path + NativeMethods.FromUtf8Native(NativeMethods.GitTreeEntryName(entry));
                    var type = NativeMethods.GitTreeEntryType(entry);
                    if (type == 2 /* GIT_OBJ_TREE */)
                    {
                        name += '/';
                    }

                    if (!pathToParent.TryGetValue(name, out var file))
                    {
                        continue;
                    }

                    var blob = NativeMethods.GitTreeEntryId(entry);
                    blobs[file.file] = blob->A;

                    if (type == 2 /* GIT_OBJ_TREE */ && trees.TryAdd(blob->A, blobs))
                    {
                        WalkTree(blob, file.file, blobs);
                    }
                }
                NativeMethods.GitObjectFree(tree);
            }
        }

        private static long GetBlob(TreeMap trees, FileMap pathToParent, long tree, string file)
        {
            var dict = trees[tree];
            if (dict.TryGetValue(file, out var blob))
            {
                return blob;
            }

            var parent = pathToParent[file].parent;
            while (parent != null)
            {
                if (parent != null && dict.TryGetValue(parent, out var parentTree))
                {
                    if (parentTree == tree)
                    {
                        return 0;
                    }
                    return GetBlob(trees, pathToParent, parentTree, file);
                }
                parent = pathToParent[parent].parent;
            }
            return 0;
        }

        private static unsafe List<GitCommit> GetCommitsByPath(string file, TreeMap trees, FileMap pathToParent, List<Commit> commits)
        {
            const int MaxParentBlob = 32;

            var contributors = new List<GitCommit>();
            var commitsToFollow = new List<(Commit commit, long blob)> { (commits[0], GetBlob(trees, pathToParent, commits[0].Tree.A, file)) };
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
                    parentBlobs[i] = GetBlob(trees, pathToParent, commit.Parents[i].Tree.A, file);
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

        private class RefComparer : EqualityComparer<string>
        {
            public static readonly RefComparer Instance = new RefComparer();

            public override bool Equals(string x, string y) => ReferenceEquals(x, y);

            public override int GetHashCode(string obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
