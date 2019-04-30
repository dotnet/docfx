// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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
        /// checkout existing git repository to specificed committish
        /// </summary>
        public static void Checkout(string path, string committish)
            => ExecuteNonQuery(path, $"-c core.longpaths=true checkout --force --progress {committish}");

        /// <summary>
        /// Clones or update a git repository to the latest version.
        /// </summary>
        public static void CloneOrUpdate(string path, string url, string committish, Config config = null)
        {
            CloneOrUpdate(path, url, new[] { committish }, bare: false, depthOne: false, prune: true, config);
            ExecuteNonQuery(path, $"-c core.longpaths=true checkout --force --progress {committish}");
        }

        /// <summary>
        /// Clones or update a git bare repository to the latest version.
        /// </summary>
        public static void CloneOrUpdateBare(string path, string url, IEnumerable<string> committishes, bool depthOne, Config config = null)
        {
            CloneOrUpdate(path, url, committishes, bare: true, depthOne, prune: true, config);
        }

        /// <summary>
        /// Fetch a git repository's updates
        /// </summary>
        public static void Fetch(string path, string url, string committish, Config config = null)
        {
            CloneOrUpdate(path, url, new[] { committish }, bare: false, depthOne: false, prune: false, config);
        }

        /// <summary>
        /// Get a list of commits using git log
        /// </summary>
        public static string[] GetCommits(string path, string committish, int top = 0)
        {
            var topParam = top > 0 ? $"-{top}" : "";
            return Execute(path, $"--no-pager log --pretty=format:\"%H\" {topParam} {committish}")
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Check if remote branch exists
        /// </summary>
        public static bool RemoteBranchExists(string remote, string branch)
        {
            try
            {
                if (GitRemoteProxy != null)
                {
                    remote = GitRemoteProxy(remote);
                }

                return Execute(".", $"ls-remote --heads \"{remote}\" {branch}").Split('\n', StringSplitOptions.RemoveEmptyEntries).Any();
            }
            catch (InvalidOperationException)
            {
                return false;
            }
       }

        /// <summary>
        /// Create a work tree for a given repo
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="committish">The commit hash, branch or tag used to create a work tree</param>
        /// <param name="path">The work tree path</param>
        public static void AddWorkTree(string cwd, string committish, string path)
        {
            // By default, add refuses to create a new working tree when <commit-ish> is a branch name and is already checked out by another working tree and remove refuses to remove an unclean working tree.
            // -f/ --force overrides these safeguards.
            ExecuteNonQuery(cwd, $"-c core.longpaths=true worktree add {path} {committish} --force");
        }

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
            var start = content.StartsWith("<<<<<<<") ? 0 : content.IndexOf("\n<<<<<<<");
            if (start >= 0 && content.Contains("\n>>>>>>>") && content.Contains("\n======="))
            {
                var line = 1;
                for (var i = 0; i <= start; i++)
                {
                    if (content[i] == '\n')
                        line++;
                }

                var source = new SourceInfo(file, line, 1);
                throw Errors.MergeConflict(source).ToException();
            }
        }

        public static unsafe bool TryGetContentFromHistory(string repoPath, string filePath, string committish, out string content)
        {
            content = null;
            if (git_repository_open(out var repo, repoPath) != 0)
            {
                return false;
            }

            if (git_revparse_single(out var commit, repo, committish) != 0)
            {
                git_repository_free(repo);
                return false;
            }

            if (git_commit_tree(out var tree, commit) != 0)
            {
                git_repository_free(repo);
                return false;
            }

            if (git_tree_entry_bypath(out var entry, tree, filePath) != 0)
            {
                git_repository_free(repo);
                return false;
            }

            var obj = git_tree_entry_id(entry);
            if (git_blob_lookup(out var blob, repo, obj) != 0)
            {
                git_tree_entry_free(entry);
                git_repository_free(repo);
                return false;
            }

            using (var stream = new UnmanagedMemoryStream((byte*)git_blob_rawcontent(blob).ToPointer(), git_blob_rawsize(blob)))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                content = reader.ReadToEnd();
            }

            git_tree_entry_free(entry);
            git_repository_free(repo);
            return true;
        }

        /// <summary>
        /// Clones or update a git repository to the latest version.
        /// </summary>
        private static void CloneOrUpdate(string path, string url, IEnumerable<string> committishes, bool bare, bool depthOne, bool prune, Config config)
        {
            // Unifies clone and fetch using a single flow:
            // - git init
            // - git remote set url
            // - git fetch
            // - git checkout (if not a bar repo)
            if (GitRemoteProxy != null && GitRemoteProxy(url) != url && Directory.Exists(path))
            {
                // optimize for test fetching
                return;
            }

            Directory.CreateDirectory(path);

            if (git_repository_init(out var repo, path, is_bare: bare ? 1 : 0) != 0)
            {
                throw new InvalidOperationException($"Cannot initialize a git repo at {path}");
            }

            Telemetry.TrackCacheTotalCount(TelemetryName.GitRepositoryCache);
            if (git_remote_create(out var remote, repo, "origin", url) == 0)
            {
                Log.Write($"Using existing repository '{path}' for '{url}'");
                Telemetry.TrackCacheMissCount(TelemetryName.GitRepositoryCache);
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
            var depth = depthOne ? "--depth 1" : "--depth 9999999999";
            var pruneSwitch = prune ? "--prune" : "";

            try
            {
                ExecuteNonQuery(path, $"{httpConfig} fetch --tags --progress --update-head-ok {pruneSwitch} {depth} \"{url}\" {refspecs}");
            }
            catch (InvalidOperationException)
            {
                // Fallback to fetch all branches and tags if the input committish is not supported by fetch
                depth = "--depth 9999999999";
                refspecs = "+refs/heads/*:refs/heads/* +refs/tags/*:refs/tags/*";
                ExecuteNonQuery(path, $"{httpConfig} fetch --tags --progress --update-head-ok {pruneSwitch} {depth} \"{url}\" {refspecs}");
            }
        }

        private static void ExecuteNonQuery(string cwd, string commandLineArgs)
        {
            Execute(cwd, commandLineArgs, stdout: false);
        }

        private static string Execute(string cwd, string commandLineArgs, bool stdout = true)
        {
            if (!Directory.Exists(cwd))
            {
                throw new DirectoryNotFoundException($"Cannot find working directory '{cwd}'");
            }

            try
            {
                return ProcessUtility.Execute("git", commandLineArgs, cwd, stdout);
            }
            catch (Win32Exception ex) when (ProcessUtility.IsExeNotFoundException(ex))
            {
                throw Errors.GitNotFound().ToException(ex);
            }
        }

        private static string GetGitCommandLineConfig(string url, Config config)
        {
            if (config is null)
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
