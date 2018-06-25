// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class GitRepoInfoProvider
    {
        private readonly ConcurrentDictionary<string, GitRepoInfo> _folderRepoInfocache
            = new ConcurrentDictionary<string, GitRepoInfo>();

        private readonly Lazy<Dictionary<string, List<GitCommit>>> _fileCommitsCache;

        private readonly GitUserProfileCache _githubUserProfileCache;

        public GitRepoInfoProvider(Docset docset, List<Document> documents)
        {
            Debug.Assert(docset != null);
            Debug.Assert(documents != null);

            _fileCommitsCache = new Lazy<Dictionary<string, List<GitCommit>>>(() => LoadCommits(docset, documents));
            var userProfileCachePath = docset.Config.Contributor.UserProfileCachePath;
            _githubUserProfileCache = GitUserProfileCache.Create(Path.Combine(docset.DocsetPath, userProfileCachePath));
        }

        public bool TryGetCommits(Document document, out List<GitCommit> commits)
        {
            Debug.Assert(document != null);

            return _fileCommitsCache.Value.TryGetValue(document.FilePath, out commits);
        }

        public GitUserProfile GetUserInformationByName(string userName)
            => _githubUserProfileCache.GetByUserName(userName);

        public GitUserProfile GetUserInformationByEmail(string userEmail)
            => _githubUserProfileCache.GetByUserEmail(userEmail);

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

        private Dictionary<string, List<GitCommit>> LoadCommits(Docset docset, List<Document> documents)
        {
            var repoRoot = GitUtility.FindRepo(Path.GetFullPath(docset.DocsetPath));
            var files = documents
                .Where(d => d.ContentType == ContentType.Markdown || d.ContentType == ContentType.SchemaDocument)
                .Select(d => PathUtility.NormalizeFile(Path.GetRelativePath(repoRoot, Path.GetFullPath(Path.Combine(docset.DocsetPath, d.FilePath)))))
                .ToList();
            var commitsList = GitUtility.GetCommits(repoRoot, files);
            var result = new Dictionary<string, List<GitCommit>>();
            for (var i = 0; i < files.Count; i++)
            {
                result[files[i]] = commitsList[i];
            }
            return result;
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
