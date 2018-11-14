// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using static Microsoft.Docs.Build.LibGit2;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide git utility using git bash
    /// </summary>
    internal static partial class GitUtility
    {
        internal static Func<string, string> GitRemoteProxy;

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
        public static Task<List<string>> ListWorkTree(string repoPath)
        {
            return Execute(repoPath, $"worktree list --porcelain", ParseWorkTreeList);

            List<string> ParseWorkTreeList(string stdout, string stderr)
            {
                Debug.Assert(stdout != null);

                // https://git-scm.com/docs/git-worktree#_porcelain_format
                var result = new List<string>();
                var isMain = true;
                foreach (var property in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var i = property.IndexOf(' ');
                    if (i > 0)
                    {
                        var key = property.Substring(0, i);
                        if (key == "worktree")
                        {
                            if (isMain)
                            {
                                // The main worktree is listed first, followed by each of the linked worktrees.
                                isMain = false;
                            }
                            else
                            {
                                result.Add(property.Substring(i + 1).Trim());
                            }
                        }
                    }
                }
                return result;
            }
        }

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
        /// </summary>
        public static unsafe string RevParse(string repoPath, string committish = null)
        {
            string result = null;

            if (string.IsNullOrEmpty(committish))
            {
                committish = "HEAD";
            }

            if (git_repository_open(out var repo, repoPath) != 0)
            {
                throw new InvalidOperationException($"Not a git repo {repoPath}");
            }

            if (git_revparse_single(out var reference, repo, committish) == 0)
            {
                result = git_object_id(reference)->ToString();
                git_object_free(reference);
            }

            git_repository_free(repo);
            return result;
        }

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
                await ExecuteNonQuery(path, $"{httpConfig} fetch --tags --prune --progress --update-head-ok \"{url}\" {refspecs}", stderr: true);
            }
            catch (InvalidOperationException ex) when (committishes.Any(rev => ex.Message.Contains(rev)))
            {
                // Fallback to fetch all branches and tags if the input committish is not supported by fetch
                refspecs = "+refs/heads/*:refs/heads/* +refs/tags/*:refs/tags/*";
                await ExecuteNonQuery(path, $"{httpConfig} fetch --tags --prune --progress --update-head-ok \"{url}\" {refspecs}");
            }
        }

        private static Task ExecuteNonQuery(string cwd, string commandLineArgs, bool stderr = false)
        {
            return Execute(cwd, commandLineArgs, (a, b) => 0, stdout: false, stderr: stderr);
        }

        private static async Task<T> Execute<T>(string cwd, string commandLineArgs, Func<string, string, T> parser, bool stdout = true, bool stderr = true)
        {
            if (!Directory.Exists(cwd))
            {
                throw new DirectoryNotFoundException($"Cannot find working directory '{cwd}'");
            }

            try
            {
                var (output, error) = await ProcessUtility.Execute("git", commandLineArgs, cwd, stdout, stderr);
                return parser(output, error);
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
