// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility
{
    using System;
    using System.IO;

    using GitSharp;

    public static class GitUtility
    {
        /// <summary>
        /// TODO: only get GitDetail on Project level?
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static GitDetail GetGitDetail(string path)
        {
            GitDetail detail = null;
            if (string.IsNullOrEmpty(path)) return detail;
            try
            {
                var repoPath = Repository.FindRepository(path);
                if (string.IsNullOrEmpty(repoPath)) return detail;

                var repo = new Repository(repoPath);

                detail = new GitDetail();
                // Convert to forward slash
                detail.LocalWorkingDirectory = repo.WorkingDirectory.BackSlashToForwardSlash();
                if (repo.Head == null) return detail;

                var branch = repo.CurrentBranch;
                detail.RemoteRepositoryUrl = repo.Config["remote.origin.url"];
                detail.RemoteBranch = branch.Name;
                // detail.Description = repo.Head.CurrentCommit.ShortHash;
                detail.RelativePath = PathUtility.MakeRelativePath(Path.GetDirectoryName(repoPath), path);
            }
            catch (Exception)
            {
                // SWALLOW exception?
                // Console.Error.WriteLine(e.Message);
            }

            return detail;
        }
    }
}
