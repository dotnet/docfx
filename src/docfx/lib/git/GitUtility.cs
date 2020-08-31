// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

using static Microsoft.Docs.Build.LibGit2;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Provide git utility using git bash
    /// </summary>
    internal static partial class GitUtility
    {
        public static PathString? FindRepository(string? path)
        {
            var repoPath = path;
            while (!string.IsNullOrEmpty(repoPath))
            {
                if (IsGitRepository(repoPath))
                {
                    return new PathString(repoPath);
                }

                repoPath = Path.GetDirectoryName(repoPath);
            }

            return null;
        }

        public static bool IsGitRepository(string path)
        {
            var gitPath = Path.Combine(path, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                if (git_repository_open(out _, gitPath) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static string[] GetFallbackBranch(string branch)
        {
            return branch switch
            {
                "master" => new string[] { "main", "master" },
                "main" => new string[] { "main", "master" },
                _ => new string[] { branch },
            };
        }

        public static unsafe (string? url, string? branch, string? commit) GetRepoInfo(string repoPath)
        {
            string? remoteName = null;
            var (url, branch, commit) = default((string, string, string));

            if (git_repository_open(out var pRepo, repoPath) != 0)
            {
                throw new ArgumentException($"Invalid git repo {repoPath}");
            }

            if (git_repository_head(out var pHead, pRepo) == 0)
            {
                commit = git_reference_target(pHead)->ToString();
                if (git_branch_name(out var pName, pHead) == 0)
                {
                    branch = Marshal.PtrToStringUTF8(pName);
                }

                remoteName = GetUpstreamRemoteName(pRepo, pHead);
                git_reference_free(pHead);
            }

            url = GetRepositoryUrl(pRepo, remoteName ?? "origin");

            git_repository_free(pRepo);

            return (url, branch, commit);

            static unsafe string? GetUpstreamRemoteName(IntPtr pRepo, IntPtr pBranch)
            {
                string? result = null;
                if (git_branch_upstream(out var pUpstream, pBranch) == 0)
                {
                    git_buf buf;
                    var pUpstreamName = git_reference_name(pUpstream);
                    if (git_branch_remote_name(&buf, pRepo, pUpstreamName) == 0)
                    {
                        result = Marshal.PtrToStringUTF8(buf.ptr) ?? "origin";
                        git_buf_free(&buf);
                    }
                    git_reference_free(pUpstream);
                }
                return result;
            }

            static unsafe string? GetRepositoryUrl(IntPtr pRepo, string remoteName)
            {
                string? result = null;
                if (git_remote_lookup(out var pRemote, pRepo, remoteName) == 0)
                {
                    result = Marshal.PtrToStringUTF8(git_remote_url(pRemote));
                    git_remote_free(pRemote);
                }
                else
                {
                    var remotes = default(git_strarray);
                    if (git_remote_list(&remotes, pRepo) == 0)
                    {
                        if (remotes.count > 0 && git_remote_lookup(out pRemote, pRepo, *remotes.strings) == 0)
                        {
                            result = Marshal.PtrToStringUTF8(git_remote_url(pRemote));
                            git_remote_free(pRemote);
                        }
                        git_strarray_free(&remotes);
                    }
                }
                return result;
            }
        }

        public static void Init(string path)
        {
            ExecuteNonQuery(path, "init");
        }

        /// <summary>
        /// Clones or update a git repository to the latest version.
        /// </summary>
        public static void AddRemote(string path, string remote, string url)
        {
            try
            {
                ExecuteNonQuery(path, $"remote add \"{remote}\" \"{url}\"");
            }
            catch (InvalidOperationException)
            {
                // Remote already exits
            }
        }

        public static void Checkout(string path, string committish, string? options = null)
        {
            ExecuteNonQuery(path, $"-c core.longpaths=true checkout --progress {options} {committish}");
        }

        public static void Fetch(PreloadConfig config, string path, string url, string refspecs, string? options = null)
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
                throw Errors.System.GitNotFound().ToException(ex);
            }
        }

        private static (string cmd, string[] secrets) GetGitCommandLineConfig(string url, PreloadConfig config)
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

            static string GetSecretFromHeader(KeyValuePair<string, string> header)
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
