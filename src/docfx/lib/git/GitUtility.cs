// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using static Microsoft.Docs.Build.LibGit2;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide git utility using git bash
    /// </summary>
    internal static partial class GitUtility
    {
        private static readonly char[] s_newline = new[] { '\r', '\n' };
        private static readonly char[] s_newlineTab = new[] { ' ', '\t' };

        internal static Func<string, string> GitRemoteProxy;

        /// <summary>
        /// Get the git remote information from remote href
        /// </summary>
        /// <param name="remoteHref">The git remote href like https://github.com/dotnet/docfx#master</param>
        public static (string remote, string refspec) GetGitRemoteInfo(string remoteHref)
        {
            Debug.Assert(!string.IsNullOrEmpty(remoteHref));

            var (path, _, fragment) = HrefUtility.SplitHref(remoteHref);

            var refspec = (string.IsNullOrEmpty(fragment) || fragment.Length <= 1) ? "master" : fragment.Substring(1);

            return (path, refspec);
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

            if (git_repository_open(out var pRepo, repoPath) != 0)
            {
                throw new ArgumentException($"Invalid git repo {repoPath}");
            }

            if (git_remote_lookup(out var pRemote, pRepo, "origin") == 0)
            {
                remote = Marshal.PtrToStringUTF8(git_remote_url(pRemote));
                git_remote_free(pRemote);
            }

            if (git_repository_head(out var pHead, pRepo) == 0)
            {
                commit = git_reference_target(pHead)->ToString();
                if (git_branch_name(out var pName, pHead) == 0)
                {
                    branch = Marshal.PtrToStringUTF8(pName);
                }
                git_reference_free(pHead);
            }

            git_repository_free(pRepo);

            return (remote, branch, commit);
        }

        /// <summary>
        /// Clones or update a git repository to the latest version.
        /// </summary>
        public static async Task CloneOrUpdate(string path, string url, string committish, Config config = null)
        {
            await CloneOrUpdate(path, url, new[] { committish }, bare: false, config);
            await ExecuteNonQuery(path, $"-c core.longpaths=true checkout --force --progress {committish}");
        }

        /// <summary>
        /// Clones or update a git bare repository to the latest version.
        /// </summary>
        public static Task CloneOrUpdateBare(string path, string url, IEnumerable<string> committishes, Config config = null)
        {
            return CloneOrUpdate(path, url, committishes, bare: true, config);
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
        /// <param name="committish">The commit hash, branch or tag used to create a work tree</param>
        /// <param name="path">The work tree path</param>
        public static Task AddWorkTree(string cwd, string committish, string path)
        {
            // By default, add refuses to create a new working tree when <commit-ish> is a branch name and is already checked out by another working tree and remove refuses to remove an unclean working tree.
            // -f/ --force overrides these safeguards.
            return ExecuteNonQuery(cwd, $"-c core.longpaths=true worktree add {path} {committish} --force");
        }

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

        /// <summary>
        /// Clones or update a git repository to the latest version.
        /// </summary>
        private static async Task CloneOrUpdate(string path, string url, IEnumerable<string> committishes, bool bare, Config config)
        {
            // Unifies clone and fetch using a single flow:
            // - git init
            // - git remote set url
            // - git fetch
            // - git checkout (if not a bar repo)
            Directory.CreateDirectory(path);

            if (git_repository_init(out var repo, path, is_bare: bare ? 1 : 0) != 0)
            {
                throw new InvalidOperationException($"Cannot initialize a git repo at {path}");
            }

            if (git_remote_create(out var remote, repo, "origin", url) == 0)
            {
                git_remote_free(remote);
            }

            git_repository_free(repo);

            // Allow test to proxy remotes to local folder
            if (GitRemoteProxy != null)
            {
                url = GitRemoteProxy(url);
            }

            var httpConfig = GetGitCommandLineConfig(url, config);
            var refspecs = string.Join(' ', committishes.Select(rev => $"+{rev}:{rev}"));

            try
            {
                await ExecuteNonQuery(path, $"{httpConfig} fetch --tags --prune --progress --update-head-ok \"{url}\" {refspecs}");
            }
            catch (InvalidOperationException)
            {
                // Fallback to fetch all branches and tags if the input committish is not supported by fetch
                refspecs = "+refs/heads/*:refs/heads/* +refs/tags/*:refs/tags/*";
                await ExecuteNonQuery(path, $"{httpConfig} fetch --tags --prune --progress --update-head-ok \"{url}\" {refspecs}");
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
            catch (Win32Exception ex) when (ProcessUtility.IsExeNotFoundException(ex))
            {
                throw Errors.GitNotFound().ToException(ex);
            }
        }

        private static string GetGitCommandLineConfig(string url, Config config)
        {
            if (config == null)
            {
                return "";
            }

            var gitConfigs =
                from http in config.Http
                where url.StartsWith(http.Key)
                from header in http.Value.Headers
                select $"-c http.{http.Key}.extraheader=\"{header.Key}: {header.Value}\"";

            return string.Join(' ', gitConfigs);
        }
    }
}
