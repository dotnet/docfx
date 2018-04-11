// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Docs
{
    /// <summary>
    /// Provide git utility using git bash
    /// </summary>
    internal static partial class GitUtility
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
        /// Clone git repository from remote to local
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="remote">The remote url</param>
        /// <param name="path">The path to clone</param>
        /// <returns>Task status</returns>
        public static Task Clone(string cwd, string remote, string path)
        {
            Debug.Assert(PathUtility.FolderPathHasInvalidChars(path));

            return ExecuteNonQuery(cwd, $"clone {remote} {path.Replace("\\", "/", StringComparison.Ordinal)}");
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
        /// </summary>
        /// <param name="cwd">The working directory</param>
        /// <returns>The git head version</returns>
        public static Task<string> HeadRevision(string cwd)
           => ExecuteQuery(cwd, "rev-parse HEAD", TimeSpan.FromMinutes(3));

        /// <summary>
        /// Get commits (per file)
        /// </summary>
        /// <param name="cwd">The current working directory</param>
        /// <param name="file">The file path, can be null</param>
        /// <param name="count">The commit count you want to retrieve, defualt is all</param>
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

            return ExecuteQuery(cwd, argumentsBuilder.ToString(), ParseListCommitOutput);
        }

        private static IReadOnlyList<GitCommit> ParseListCommitOutput(string lines)
            => (from line in lines.Split(s_newline, StringSplitOptions.RemoveEmptyEntries)
                let parts = line.Split('|')
                select new GitCommit { Sha = parts[0], Time = DateTimeOffset.Parse(parts[1], null), AuthorName = parts[2], AuthorEmail = parts[3] }).ToList();

        private static Task ExecuteNonQuery(string cwd, string commandLineArgs, TimeSpan? timeout = null)
            => Execute(cwd, commandLineArgs, timeout, x => x);

        private static Task<T> ExecuteQuery<T>(string cwd, string commandLineArgs, Func<string, T> parser, TimeSpan? timeout = null)
            => Execute(cwd, commandLineArgs, timeout, parser);

        private static Task<string> ExecuteQuery(string cwd, string commandLineArgs, TimeSpan? timeout = null)
            => Execute(cwd, commandLineArgs, timeout, x => x);

        private static async Task<T> Execute<T>(string cwd, string commandLineArgs, TimeSpan? timeout, Func<string, T> parser)
        {
            Debug.Assert(!string.IsNullOrEmpty(cwd));
            Debug.Assert(!PathUtility.FolderPathHasInvalidChars(cwd));

            // todo: check git exist or not
            var response = await ProcessUtility.Execute("git", commandLineArgs, cwd, timeout);
            return parser(response);
        }
    }
}
