// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        /// Clone git repository from remote to local
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="remote">The remote url</param>
        /// <param name="path">The path to clone</param>
        /// <param name="branch">The branch you want to clone</param>
        /// <param name="bare">Make the git repo bare</param>
        /// <returns>Task status</returns>
        public static Task Clone(string cwd, string remote, string path, string branch = null, bool bare = true)
        {
            Directory.CreateDirectory(cwd);
            var cmd = string.IsNullOrEmpty(branch)
                ? $"clone {remote} \"{path.Replace("\\", "/")}\""
                : $"clone -b {branch} --single-branch {remote} \"{path.Replace("\\", "/")}\"";

            if (bare)
                cmd += " --bare";

            return ExecuteNonQuery(cwd, cmd, null, (outputLine, isError) => DefaultOutputHandler(outputLine, false) /*git clone always put progress to standard error*/);
        }

        /// <summary>
        /// Fetch update from remote
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <returns>Task status</returns>
        public static Task Fetch(string cwd)
            => ExecuteNonQuery(cwd, "fetch");

        /// <summary>
        /// Pull update from remote
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="remote">The remote name, default is origin</param>
        /// <returns>Task status</returns>
        public static Task Pull(string cwd, string remote = null)
            => ExecuteNonQuery(cwd, $"pull {remote ?? string.Empty}");

        /// <summary>
        /// Checkout repo to specified branch
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="create">Create this branch if not exists or not</param>
        /// <param name="branch">The branch name, default is master</param>
        /// <returns>Task status</returns>
        public static Task Checkout(string cwd, bool create, string branch = null)
            => ExecuteNonQuery(cwd, $"checkout {(create ? "-b" : "")} {branch ?? "master"}", TimeSpan.FromMinutes(10));

        /// <summary>
        /// Reset(hard) current repo to remote branch
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="branch">The remote branch name</param>
        /// <returns>Task status</returns>
        public static Task Reset(string cwd, string branch)
        {
            Debug.Assert(!string.IsNullOrEmpty(branch));

            return ExecuteNonQuery(cwd, $"reset --hard origin/{branch}", TimeSpan.FromMinutes(10));
        }

        /// <summary>
        /// List work trees for given repo
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        public static Task<List<string>> ListWorkTrees(string cwd)
            => ExecuteQuery(
                cwd,
                $"worktree list",
                lines =>
                {
                    var result = new List<string>();
                    if (string.IsNullOrEmpty(lines))
                    {
                        return result;
                    }

                    var worktreeLines = lines.Split(s_newline, StringSplitOptions.RemoveEmptyEntries);
                    return worktreeLines.Select(s => s.Split(s_newlineTab, StringSplitOptions.RemoveEmptyEntries)[0]).ToList();
                },
                TimeSpan.FromSeconds(30));

        /// <summary>
        /// Create a work tree for an given repo
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="commitHash">The commit hash you want to use to create a work tree</param>
        /// <param name="path">The work tree path</param>
        public static Task CreateWorkTree(string cwd, string commitHash, string path)
            => ExecuteNonQuery(cwd, $"worktree add {path} {commitHash}");

        /// <summary>
        /// Remove a work tree for an given repo
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="path">The to-be-removed work tree path</param>
        public static Task RemoveWorkTree(string cwd, string path)
            => ExecuteNonQuery(cwd, $"worktree remove -f {path}");

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
           => ExecuteQuery(cwd, $"rev-parse {branch}", TimeSpan.FromMinutes(3));

        private static Task ExecuteNonQuery(string cwd, string commandLineArgs, TimeSpan? timeout = null, Action<string, bool> outputHandler = null)
            => Execute(cwd, commandLineArgs, timeout, x => x, outputHandler ?? DefaultOutputHandler);

        private static Task<T> ExecuteQuery<T>(string cwd, string commandLineArgs, Func<string, T> parser, TimeSpan? timeout = null, Action<string, bool> outputHandler = null)
            => Execute(cwd, commandLineArgs, timeout, parser, outputHandler);

        private static Task<string> ExecuteQuery(string cwd, string commandLineArgs, TimeSpan? timeout = null, Action<string, bool> outputHandler = null)
            => Execute(cwd, commandLineArgs, timeout, x => x, outputHandler);

        private static async Task<T> Execute<T>(string cwd, string commandLineArgs, TimeSpan? timeout, Func<string, T> parser, Action<string, bool> outputHandler)
        {
            Debug.Assert(!string.IsNullOrEmpty(cwd));

            // todo: check git exist or not
            var response = await ProcessUtility.Execute("git", commandLineArgs, cwd, timeout, outputHandler);
            return parser(response);
        }

        private static void DefaultOutputHandler(string outputLine, bool isError)
        {
            if (string.IsNullOrEmpty(outputLine))
            {
                return;
            }

            if (isError)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(outputLine);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(outputLine);
            }
        }
    }
}
