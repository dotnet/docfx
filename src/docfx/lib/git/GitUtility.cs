// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// <returns>Task status</returns>
        public static Task Clone(string cwd, string remote, string path, string branch = null)
        {
            Directory.CreateDirectory(cwd);
            var cmd = string.IsNullOrEmpty(branch)
                ? $"clone {remote} \"{path.Replace("\\", "/")}\""
                : $"clone -b {branch} --single-branch {remote} \"{path.Replace("\\", "/")}\"";

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
        /// Retrieve git head version
        /// TODO: For testing purpose only, move it to test
        /// </summary>
        public static Task<string> HeadRevision(string cwd)
           => ExecuteQuery(cwd, "rev-parse HEAD", TimeSpan.FromMinutes(3));

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
