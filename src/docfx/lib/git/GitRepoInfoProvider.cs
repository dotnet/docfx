// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class GitRepoInfoProvider
    {
        private readonly ConcurrentDictionary<string, GitRepoInfo> _folderRepoInfocache
            = new ConcurrentDictionary<string, GitRepoInfo>();

        private readonly ConcurrentDictionary<string, List<GitCommit>> _fileCommitsCache;

        private readonly GitUserProfileCache _githubUserProfileCache = null;

        public bool ProfileInitialized => _githubUserProfileCache != null;

        public GitRepoInfoProvider(Docset docset, List<Document> documents)
        {
            Debug.Assert(docset != null);
            Debug.Assert(documents != null);

            var repoRoot = GitUtility.FindRepo(Path.GetFullPath(docset.DocsetPath));
            var files = documents.Select(d => PathUtility.NormalizeFile(Path.GetRelativePath(repoRoot, Path.GetFullPath(Path.Combine(docset.DocsetPath, d.FilePath))))).ToList();
            var commitsList = GitUtility.GetCommits(repoRoot, files);
            _fileCommitsCache = new ConcurrentDictionary<string, List<GitCommit>>(
                documents.Zip(commitsList, (d, c) => new KeyValuePair<string, List<GitCommit>>(d.FilePath, c)));
            var userProfileCachePath = docset.Config.Contributor.UserProfileCachePath;
            if (!string.IsNullOrEmpty(userProfileCachePath))
                _githubUserProfileCache = GitUserProfileCache.Create(Path.Combine(docset.DocsetPath, userProfileCachePath));
        }

        public bool TryGetCommits(Document document, out List<GitCommit> commits)
        {
            Debug.Assert(document != null);

            return _fileCommitsCache.TryGetValue(document.FilePath, out commits);
        }

        public GitUserProfile GetUserInformationByName(string userName)
        {
            Debug.Assert(ProfileInitialized);

            return _githubUserProfileCache.GetByUserName(userName);
        }

        public GitUserProfile GetUserInformationByEmail(string userEmail)
        {
            Debug.Assert(ProfileInitialized);

            return _githubUserProfileCache.GetByUserEmail(userEmail);
        }

        public GitRepoInfo GetGitRepoInfo(Document document)
        {
            Debug.Assert(document != null);

            return GetFolderGitRepoInfo(Path.GetDirectoryName(Path.Combine(document.Docset.DocsetPath, document.FilePath)));
        }

        public GitRepoInfo GetGitRepoInfo(string filePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(filePath));

            return GetFolderGitRepoInfo(Path.GetDirectoryName(Path.GetFullPath(filePath)));
        }

        private GitRepoInfo GetFolderGitRepoInfo(string folder)
        {
            Debug.Assert(!string.IsNullOrEmpty(folder));
            Debug.Assert(Directory.Exists(folder));

            folder = PathUtility.NormalizeFile(folder);
            return _folderRepoInfocache.GetOrAdd(folder, GetFolderGitRepoInfoCore);
        }

        private GitRepoInfo GetFolderGitRepoInfoCore(string folder)
        {
            if (GitUtility.IsRepo(folder))
                return GitRepoInfo.CreateAsync(folder).Result;

            var parent = folder.Substring(0, folder.LastIndexOf("/", System.StringComparison.Ordinal));
            return Directory.Exists(parent)
                ? _folderRepoInfocache.GetOrAdd(parent, GetFolderGitRepoInfoCore)
                : null;
        }
    }
}
