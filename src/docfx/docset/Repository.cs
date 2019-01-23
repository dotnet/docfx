// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class Repository
    {
        public string Remote { get; private set; }

        public string Branch { get; private set; }

        public string Commit { get; private set; }

        public string Path { get; private set; }

        private Repository(string remote, string branch, string commit, string path)
        {
            Remote = remote ?? throw new ArgumentNullException(nameof(remote));
            Branch = branch ?? "master";
            Commit = commit ?? throw new ArgumentNullException(nameof(commit));
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>
        /// Our repository's branch info should NOT depend on git, unless you are pretty sure about that
        /// </summary>
        public static Repository Create(string path, string branch)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            var repoPath = GitUtility.FindRepo(path);

            if (repoPath == null)
                return null;

            var (remote, repoBranch, commit) = GitUtility.GetRepoInfo(repoPath);
            var gitIndex = remote.IndexOf(".git");
            if (gitIndex >= 0)
            {
                remote = remote.Remove(gitIndex);
            }
            return new Repository(remote, branch ?? repoBranch, commit, PathUtility.NormalizeFolder(repoPath));
        }
    }
}
