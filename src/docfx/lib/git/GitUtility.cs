// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide git utility using git bash
    /// </summary>
    internal static partial class GitUtility
    {
        private static readonly char[] s_newline = new[] { '\r', '\n' };
        private static readonly char[] s_newlineTab = new[] { ' ', '\t' };

        /// <summary>
        /// Get the git remote information from remote href
        /// </summary>
        /// <param name="remoteHref">The git remote href like https://github.com/dotnet/docfx#master</param>
        public static (string url, string branch) GetGitRemoteInfo(string remoteHref)
        {
            Debug.Assert(!string.IsNullOrEmpty(remoteHref));

            var (path, _, fragment) = HrefUtility.SplitHref(remoteHref);

            var refSpec = (string.IsNullOrEmpty(fragment) || fragment.Length <= 1) ? "master" : fragment.Substring(1);
            var uri = new Uri(path);
            var url = uri.GetLeftPart(UriPartial.Path);

            return (url, refSpec);
        }

        /// <summary>
        /// Find git repo directory
        /// </summary>
        /// <param name="path">The git repo entry point</param>
        /// <returns>The git repo root path. null if the repo root is not found</returns>
        public static string FindRepo(string path)
        {
            var repo = path;
            while (!string.IsNullOrEmpty(repo))
            {
                if (IsRepo(repo))
                {
                    return repo;
                }

                repo = Path.GetDirectoryName(repo);
            }

            return string.IsNullOrEmpty(repo) ? null : repo;
        }

        /// <summary>
        /// Determine if the path is a git repo
        /// </summary>
        /// <param name="path">The repo path</param>
        /// <returns>Is git repo or not</returns>
        public static bool IsRepo(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            var gitPath = Path.Combine(path, ".git");

            return Directory.Exists(gitPath) || File.Exists(gitPath) /* submodule */;
        }

        /// <summary>
        /// Retrieve git repo information.
        /// </summary>
        public static unsafe (string remote, string branch, string commit) GetRepoInfo(string repoPath)
        {
            var (remote, branch, commit) = default((string, string, string));
            var pRepo = OpenRepo(repoPath);

            // TODO: marshal strings
            fixed (byte* pRemoteName = LibGit2.ToUtf8Native("origin"))
            {
                if (LibGit2.GitRemoteLookup(out var pRemote, pRepo, pRemoteName) == 0)
                {
                    remote = LibGit2.FromUtf8Native(LibGit2.GitRemoteUrl(pRemote));
                    LibGit2.GitRemoteFree(pRemote);
                }
            }

            if (LibGit2.GitRepositoryHead(out var pHead, pRepo) == 0)
            {
                commit = LibGit2.GitReferenceTarget(pHead)->ToString();
                if (LibGit2.GitBranchName(out var pName, pHead) == 0)
                {
                    branch = LibGit2.FromUtf8Native(pName);
                }
                LibGit2.GitReferenceFree(pHead);
            }

            LibGit2.GitRepositoryFree(pRepo);

            return (remote, branch, commit);
        }

        public static unsafe IntPtr OpenRepo(string path)
        {
            fixed (byte* pRepoPath = LibGit2.ToUtf8Native(path))
            {
                if (LibGit2.GitRepositoryOpen(out var repo, pRepoPath) != 0)
                {
                    throw new ArgumentException($"Invalid git repo {path}");
                }

                return repo;
            }
        }

        /// <summary>
        /// Clone git repository from remote to local
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="remote">The remote url</param>
        /// <param name="path">The path to clone</param>
        /// <param name="branch">The branch you want to clone</param>
        /// <param name="bare">Make the git repo bare</param>
        /// <returns>Task status</returns>
        public static async Task Clone(string cwd, string remote, string path, string branch = null, bool bare = false, string gitConfig = null)
        {
            Directory.CreateDirectory(cwd);

            // clone with configuration core.longpaths turned-on
            // https://stackoverflow.com/questions/22575662/filename-too-long-in-git-for-windows#answer-40909460
            var cmd = string.IsNullOrEmpty(branch)
                ? $" {gitConfig} clone -c core.longpaths=true {remote} \"{path.Replace("\\", "/")}\""
                : $" {gitConfig} clone -c core.longpaths=true -b {branch} --single-branch {remote} \"{path.Replace("\\", "/")}\"";

            if (bare)
                cmd += " --bare";

            await ExecuteNonQuery(cwd, cmd);
        }

        /// <summary>
        /// Fetch update from remote
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <returns>Task status</returns>
        public static async Task Fetch(string cwd, string remote, string refSpec, string gitConfig = null)
        {
            // -c must come before `fetch`
            await ExecuteNonQuery(cwd, $"{gitConfig} fetch {remote} {refSpec}");
        }

        /// <summary>
        /// List work trees for a given repo
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        public static Task<List<string>> ListWorkTrees(string cwd, bool includeMain)
            => ExecuteQuery(
                cwd,
                $"worktree list",
                lines =>
                {
                    Debug.Assert(lines != null);
                    var worktreeLines = lines.Split(s_newline, StringSplitOptions.RemoveEmptyEntries);
                    var workTreePaths = new List<string>();

                    var i = 0;
                    foreach (var workTreeLine in worktreeLines)
                    {
                        if (i++ > 0 || includeMain)
                        {
                            // The main worktree is listed first, followed by each of the linked worktrees.
                            workTreePaths.Add(workTreeLine.Split(s_newlineTab, StringSplitOptions.RemoveEmptyEntries)[0]);
                        }
                    }

                    return workTreePaths;
                });

        /// <summary>
        /// Create a work tree for a given repo
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="commitHash">The commit hash you want to use to create a work tree</param>
        /// <param name="path">The work tree path</param>
        public static Task AddWorkTree(string cwd, string commitHash, string path)
            => ExecuteNonQuery(cwd, $"worktree add {path} {commitHash}");

        /// <summary>
        /// Prune work trees which are not connected with an given repo
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        public static Task PruneWorkTrees(string cwd)
            => ExecuteNonQuery(cwd, $"worktree prune");

        /// <summary>
        /// Retrieve git head version
        /// TODO: For testing purpose only, move it to test
        /// </summary>
        public static Task<string> Revision(string cwd, string branch = "HEAD")
           => ExecuteQuery(cwd, $"rev-parse {branch}");

        public static void CheckMergeConflictMarker(string content, string file)
        {
            if ((content.StartsWith("<<<<<<<") || content.Contains("\n<<<<<<<")) &&
                content.Contains("\n>>>>>>>") &&
                content.Contains("\n======="))
            {
                throw Errors.MergeConflict(file).ToException();
            }
        }

        private static Task ExecuteNonQuery(string cwd, string commandLineArgs)
            => Execute(cwd, commandLineArgs, x => x, redirectOutput: false);

        private static Task<T> ExecuteQuery<T>(string cwd, string commandLineArgs, Func<string, T> parser)
            => Execute(cwd, commandLineArgs, parser, redirectOutput: true);

        private static Task<string> ExecuteQuery(string cwd, string commandLineArgs)
            => Execute(cwd, commandLineArgs, x => x, redirectOutput: true);

        private static async Task<T> Execute<T>(string cwd, string commandLineArgs, Func<string, T> parser, bool redirectOutput)
        {
            if (!Directory.Exists(cwd))
            {
                throw new DirectoryNotFoundException($"Cannot find working directory '{cwd}'");
            }

            try
            {
                return parser(await ProcessUtility.Execute("git", commandLineArgs, cwd, redirectOutput));
            }
            catch (Win32Exception ex) when (ProcessUtility.IsNotFound(ex))
            {
                throw Errors.GitNotFound().ToException(ex);
            }
        }
    }
}
