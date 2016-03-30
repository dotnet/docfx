// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Utility.Git;

    using Repository = GitSharp.Repository;
    using CoreRepository = GitSharp.Core.Repository;

    public static class GitUtility
    {
        private static readonly ConcurrentDictionary<string, RepoWrapper> RepoCache = new ConcurrentDictionary<string, RepoWrapper>();
        private static readonly ConcurrentDictionary<string, GitDetail> Cache = new ConcurrentDictionary<string, GitDetail>();
        
        /// <summary>
        /// TODO: only get GitDetail on Project level?
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static GitDetail GetGitDetail(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return Cache.GetOrAdd(path, s => GetGitDetailCore(s));
        }
        
        private static GitDetail GetGitDetailCore(string path)
        {
            GitDetail detail = null;
            try
            {
                var repoPath = Repository.FindRepository(path);
                if (string.IsNullOrEmpty(repoPath))
                {
                    // Use local modified date when git repo is not found
                    var time = File.GetLastWriteTimeUtc(path);
                    detail = new GitDetail
                    {
                        CommitDetail = new CommitDetail
                        {
                            Author = new UserInfo
                            {
                                Date = time,
                            }
                        }
                    };
                    return detail;
                }

                var directory = Path.GetDirectoryName(repoPath);
                var wrapper = RepoCache.GetOrAdd(directory, s => new RepoWrapper(CoreRepository.Open(s)));
                var repo = wrapper.Repo;
                path = PathUtility.MakeRelativePath(directory, path);
                detail = new GitDetail();

                var walker = wrapper.Walker;
                var commitDetail = walker.GetCommitDetail(path);
                detail.CommitDetail = commitDetail;

                // Convert to forward slash
                detail.LocalWorkingDirectory = repo.WorkingDirectory.FullName;
                if (repo.Head == null) return detail;

                var branch = repo.getBranch();
                detail.RemoteRepositoryUrl = repo.Config.getString("remote", "origin", "url");
                detail.RemoteBranch = branch;
                detail.RelativePath = path;
                return detail;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Have issue extracting repo detail for {path}: {e.Message}");
            }

            return detail;
        }

        private sealed class RepoWrapper
        {
            public CoreRepository Repo { get; set; }
            public RepositoryWalker Walker { get; set; }
            public RepoWrapper(CoreRepository repo)
            {
                Repo = repo;
                Walker = new RepositoryWalker(repo);
            }
        }
    }
}
