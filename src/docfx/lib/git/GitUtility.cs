// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

using static Microsoft.Docs.Build.LibGit2;

#nullable enable

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide git utility using git bash
    /// </summary>
    internal static partial class GitUtility
    {
        /// <summary>
        /// Find git repo directory
        /// </summary>
        /// <param name="path">The git repo entry point</param>
        /// <returns>The git repo root path. null if the repo root is not found</returns>
        public static string? FindRepo(string? path)
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
        public static unsafe (string? remote, string? branch, string? commit) GetRepoInfo(string repoPath)
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
        public static void Init(string path)
        {
            ExecuteNonQuery(path, "init");
        }

        /// <summary>
        /// checkout existing git repository to specificed committish
        /// </summary>
        public static void Checkout(string path, string committish, string? options = null)
        {
            ExecuteNonQuery(path, $"-c core.longpaths=true checkout --progress {options} {committish}");
        }

        /// <summary>
        /// Fetch a git repository's updates
        /// </summary>
        public static void Fetch(Config config, string path, string url, string refspecs, string? options = null)
        {
            // Allow test to proxy remotes to local folder
            url = TestQuirks.GitRemoteProxy?.Invoke(url) ?? url;

            var (http, secrets) = GetGitCommandLineConfig(url, config);

            ExecuteNonQuery(path, $"{http} fetch --progress {options} \"{url}\" {refspecs}", secrets);
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

        public static unsafe byte[]? ReadBytes(string repoPath, string filePath, string committish)
        {
            if (git_repository_open(out var repo, repoPath) != 0)
            {
                return null;
            }

            if (git_revparse_single(out var commit, repo, committish) != 0)
            {
                git_repository_free(repo);
                return null;
            }

            if (git_commit_tree(out var tree, commit) != 0)
            {
                git_repository_free(repo);
                return null;
            }

            if (git_tree_entry_bypath(out var entry, tree, filePath) != 0)
            {
                git_repository_free(repo);
                return null;
            }

            var obj = git_tree_entry_id(entry);
            if (git_blob_lookup(out var blob, repo, obj) != 0)
            {
                git_tree_entry_free(entry);
                git_repository_free(repo);
                return null;
            }

            var blobSize = git_blob_rawsize(blob);
            var bytes = new Span<byte>(git_blob_rawcontent(blob).ToPointer(), blobSize);
            var result = new byte[blobSize];

            bytes.CopyTo(result);

            git_tree_entry_free(entry);
            git_repository_free(repo);

            return result;
        }

        private static void ExecuteNonQuery(string cwd, string commandLineArgs, string[]? secrets = null)
        {
            Execute(cwd, commandLineArgs, stdout: false, secrets);
        }

        private static string Execute(string cwd, string commandLineArgs, bool stdout = true, string[]? secrets = null)
        {
            if (!Directory.Exists(cwd))
            {
                throw new DirectoryNotFoundException($"Cannot find working directory '{cwd}'");
            }

            try
            {
                return ProcessUtility.Execute("git", commandLineArgs, cwd, stdout, secrets);
            }
            catch (Win32Exception ex) when (ProcessUtility.IsExeNotFoundException(ex))
            {
                throw Errors.GitNotFound().ToException(ex);
            }
        }

        private static (string cmd, string[] secrets) GetGitCommandLineConfig(string url, Config config)
        {
            if (config is null)
            {
                return default;
            }

            var gitConfigs = (
                from http in config.Http
                where url.StartsWith(http.Key)
                from header in http.Value.Headers
                select (cmd: $"-c http.extraheader=\"{header.Key}: {header.Value}\"", secret: GetSecretFromHeader(header))).ToArray();

            return (string.Join(' ', gitConfigs.Select(item => item.cmd)), gitConfigs.Select(item => item.secret).ToArray());

            string GetSecretFromHeader(KeyValuePair<string, string> header)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) &&
                    AuthenticationHeaderValue.TryParse(header.Value, out var value))
                {
                    return value.Parameter;
                }
                return header.Value;
            }
        }
    }
}
