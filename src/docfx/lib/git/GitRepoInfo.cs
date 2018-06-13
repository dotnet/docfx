// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        public string HeadCommitId { get; set; }

        public string RootPath { get; set; }

        public static async Task<GitRepoInfo> CreateAsync(string cwd)
        {
            Debug.Assert(GitUtility.IsRepo(cwd));
            Debug.Assert(Path.IsPathRooted(cwd));

            var url = await GitUtility.GetOriginalUrl(cwd);

            // TODO: support VSTS, or others
            var match = GitHubRepoUrlRegex.Match(url);
            if (!match.Success)
                return null;

            return new GitRepoInfo
            {
                Host = GitHost.GitHub,
                Account = match.Groups["account"].Value,
                Name = match.Groups["repository"].Value,
                Branch = await GitUtility.GetLocalBranch(cwd), // TODO: handle detached HEAD/submodule/sourceBranchName
                HeadCommitId = await GitUtility.GetLocalBranchCommitId(cwd),
                RootPath = cwd,
            };
        }

        /// <summary>
        /// Retrieve permanent git URL
        /// </summary>
        /// <param name="path">Path relative to <see cref="RootPath"/> </param>
        public string GetGitPermaLink(string path)
        {
            Debug.Assert(Host == GitHost.GitHub);
            return $"https://github.com/{Account}/{Name}/blob/{HeadCommitId}/{path}";
        }

        /// <summary>
        /// Retrieve git URL
        /// </summary>
        /// <param name="path">Path relative to <see cref="RootPath"/> </param>
        public string GetGitLink(string path)
        {
            Debug.Assert(Host == GitHost.GitHub);
            return $"https://github.com/{Account}/{Name}/blob/{Branch}/{path}";
        }
    }
}
