// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal class GitRepoInfo
    {
        private static readonly Regex GitHubRepoUrlRegex =
            new Regex(
                @"^((https|http):\/\/(.+@)?github\.com\/|git@github\.com:)(?<account>\S+)\/(?<repository>[A-Za-z0-9_.-]+)(\.git)?\/?$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

        public GitHost Host { get; set; }

        public string Account { get; set; }

        public string Name { get; set; }

        public string Branch { get; set; }

        public string Commit { get; set; }

        public string RootPath { get; set; }

        public static GitRepoInfo Create(string cwd)
        {
            Debug.Assert(GitUtility.IsRepo(cwd));
            Debug.Assert(Path.IsPathRooted(cwd));

            var (host, account, repository) = default((GitHost, string, string));
            var (remote, branch, commit) = GitUtility.GetRepoInfo(cwd);

            // TODO: support VSTS, or others
            // TODO: fallback branch to environment variable to support CIs
            if (!string.IsNullOrEmpty(remote))
            {
                var match = GitHubRepoUrlRegex.Match(remote);
                if (match.Success)
                {
                    account = match.Groups["account"].Value;
                    repository = match.Groups["repository"].Value;
                    host = GitHost.GitHub;
                }
            }

            return new GitRepoInfo
            {
                Host = host,
                Account = account,
                Name = repository,
                Branch = branch,
                Commit = commit,
                RootPath = cwd,
            };
        }
    }
}
