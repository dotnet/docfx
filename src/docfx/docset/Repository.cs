// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal class Repository
    {
        public string Remote { get; }

        public string Branch { get; }

        public string Commit { get; }

        public string Path { get; }

        private Repository(string remote, string branch, string commit, string path)
        {
            Remote = remote ?? throw new ArgumentNullException(nameof(remote));
            Branch = branch ?? "master";
            Commit = commit ?? throw new ArgumentNullException(nameof(commit));
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>
        /// Create repository from environment variable(remote + branch), fallback to git info if they are not set
        /// </summary>
        public static Repository Create(string path)
        {
            return Create(path, EnvironmentVariable.RepositoryBranch, EnvironmentVariable.RepositoryUrl);
        }

        /// <summary>
        /// Repository's branch info ashould NOT depend on git, unless you are pretty sure about that
        /// Repository's url can also be overwritten
        /// </summary>
        public static Repository Create(string path, string branch, string repoUrl = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            var repoPath = GitUtility.FindRepo(System.IO.Path.GetFullPath(path));

            if (repoPath is null)
                return null;

            var (remote, repoBranch, commit) = GitUtility.GetRepoInfo(repoPath);

            // remove user name, token and .git from url like https://xxxxx@dev.azure.com/xxxx.git
            remote = Regex.Replace(repoUrl ?? remote, @"^((http|https):\/\/)?([^\/\s]+@)?([\S]+?)(\.git)?$", "$1$4");

            return new Repository(remote, branch ?? repoBranch, commit, PathUtility.NormalizeFolder(repoPath));
        }
    }
}
