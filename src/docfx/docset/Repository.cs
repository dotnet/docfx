// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal class Repository
    {
        private static readonly Regex s_gitHubRepoUrlRegex =
            new Regex(
                @"^((https|http):\/\/(.+@)?github\.com\/|git@github\.com:)(?<account>\S+)\/(?<repository>[A-Za-z0-9_.-]+)(\.git)?\/?$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

        public GitHost Host { get; }

        public string Name { get; }

        public string Branch { get; }

        public string Commit { get; }

        public string RepositoryPath { get; }

        private Repository(string path)
        {
            Debug.Assert(GitUtility.IsRepo(path));
            Debug.Assert(Path.IsPathRooted(path));

            var (host, account, repository) = default((GitHost, string, string));
            var (remote, branch, commit) = GitUtility.GetRepoInfo(path);

            // TODO: support VSTS, or others
            if (!string.IsNullOrEmpty(remote))
            {
                var match = s_gitHubRepoUrlRegex.Match(remote);
                if (match.Success)
                {
                    account = match.Groups["account"].Value;
                    repository = match.Groups["repository"].Value;
                    host = GitHost.GitHub;
                }
            }

            Host = host;
            Name = $"{account}/{repository}";
            Branch = branch;
            Commit = commit;
            RepositoryPath = PathUtility.NormalizeFolder(path);
        }

        public static Repository Create(string path)
        {
            Debug.Assert(Path.IsPathRooted(path));

            var repoPath = GitUtility.FindRepo(path);
            return repoPath != null ? new Repository(repoPath) : null;
        }
    }
}
