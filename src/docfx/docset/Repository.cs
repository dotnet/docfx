// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class Repository
    {
        public GitHost Host { get; private set; }

        public string Remote { get; private set; }

        public string Branch { get; private set; }

        public string Commit { get; private set; }

        public string Path { get; private set; }

        private Repository(GitHost host, string remote, string branch, string commit = null, string path = null)
        {
            Host = host;
            Remote = remote ?? throw new ArgumentNullException(nameof(remote));
            Branch = branch ?? "master";
            Commit = commit;
            Path = path;
        }

        public static Repository CreateFromFolder(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            var repoPath = GitUtility.FindRepo(path);

            if (repoPath == null)
                return null;

            var (remote, branch, commit) = GitUtility.GetRepoInfo(repoPath);
            if (GitHostUtility.TryParse(remote, out var githost))
            {
                var gitIndex = remote.IndexOf(".git");
                if (gitIndex >= 0)
                {
                    remote = remote.Remove(gitIndex);
                }
                return new Repository(githost, remote, branch, commit, PathUtility.NormalizeFolder(repoPath));
            }

            return null;
        }

        public static Repository CreateFromRemote(string remote)
        {
            if (string.IsNullOrEmpty(remote))
                return null;

            if (GitHostUtility.TryParse(remote, out var gitHost))
            {
                var (url, branch) = GitUtility.GetGitRemoteInfo(remote);
                return new Repository(gitHost, url, branch);
            }

            return null;
        }
    }
}
