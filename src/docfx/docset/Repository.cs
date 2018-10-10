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

        public string Owner { get; }

        public string Name { get; }

        public string FullName => $"{Owner}/{Name}";

        public string Branch { get; }

        public string Commit { get; }

        public string RepositoryPath { get; }

        private Repository(GitHost host, string account, string repository, string branch, string commit, string path)
        {
            Host = host;
            Owner = account;
            Name = repository;
            Branch = branch ?? "master";
            Commit = commit;
            RepositoryPath = PathUtility.NormalizeFolder(path);
        }

        public Repository With(string owner, string name, string branch = null)
        {
            return new Repository(Host, owner ?? Owner, name ?? Name, branch ?? Branch, Commit, RepositoryPath);
        }

        public string GetRemoteWithBranch()
        {
            switch (Host)
            {
                case GitHost.GitHub:
                    return $"https://github.com/{Owner}/{Name}#{Branch}";
                default:
                    throw new System.NotSupportedException($"{Host} is not supported yet");
            }
        }

        public static Repository Create(string path)
        {
            Debug.Assert(Path.IsPathRooted(path));

            var repoPath = GitUtility.FindRepo(path);

            if (repoPath == null)
                return null;

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

            return new Repository(host, account, repository, branch, commit, repoPath);
        }
    }
}
