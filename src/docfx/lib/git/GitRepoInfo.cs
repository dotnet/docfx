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

            var (remote, branch, commit) = GitUtility.GetRepoInfo(cwd);

            // TODO: support VSTS, or others
            var match = GitHubRepoUrlRegex.Match(remote);
            if (!match.Success)
                return null;

            return new GitRepoInfo
            {
                Host = GitHost.GitHub,
                Account = match.Groups["account"].Value,
                Name = match.Groups["repository"].Value,
                Branch = branch,
                Commit = commit,
                RootPath = cwd,
            };
        }
    }
}
