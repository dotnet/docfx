// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class GitRepoInfoProvider
    {
        private static readonly string s_defaultCachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".docfx",
            "cache",
            "user-profile.json");

        private readonly ConcurrentDictionary<string, GitRepoInfo> _folderRepoInfocache
            = new ConcurrentDictionary<string, GitRepoInfo>();

        private readonly Lazy<Dictionary<string, List<GitCommit>>> _fileCommitsCache;

        private readonly GitUserProfileCache _githubUserProfileCache;

        public GitRepoInfoProvider(Docset docset)
        {
            Debug.Assert(docset != null);

            _fileCommitsCache = new Lazy<Dictionary<string, List<GitCommit>>>(() => LoadCommits(docset));
            var userProfileCachePath = string.IsNullOrEmpty(docset.Config.Contributor.UserProfileCachePath)
                ? s_defaultCachePath
                : Path.Combine(docset.DocsetPath, docset.Config.Contributor.UserProfileCachePath);
            _githubUserProfileCache = GitUserProfileCache.Create(userProfileCachePath);
        }

        public (GitUserInfo author, GitUserInfo[] contributors, DateTime updatedAt) GetContributorInfo(Document document)
        {
            Debug.Assert(document != null);

            // TODO: support specifed authorName and updatedAt
            GitUserProfile authorInfo = null;
            if (!_fileCommitsCache.Value.TryGetValue(document.FilePath, out var commits)
                || commits.Count == 0)
            {
                return (null, null, DateTime.Now);
            }
            for (var i = commits.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(commits[i].AuthorEmail))
                {
                    authorInfo = _githubUserProfileCache.GetByUserEmail(commits[i].AuthorEmail);
                    if (authorInfo != null)
                        break;
                }
            }
            var contributors = (from commit in commits
                                where !string.IsNullOrEmpty(commit.AuthorEmail)
                                let info = _githubUserProfileCache.GetByUserEmail(commit.AuthorEmail)
                                where info != null
                                group info by info.Id into g
                                select g.First()).ToArray();

            // TODO: support read build history
            var updatedAt = DateTime.Now;

            return (ToGitUserInfo(authorInfo), contributors.Select(ToGitUserInfo).ToArray(), updatedAt);
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

        private bool TryGetCommits(Document document, out List<GitCommit> commits)
        {
            Debug.Assert(document != null);

            return _fileCommitsCache.Value.TryGetValue(document.FilePath, out commits);
        }

        private Dictionary<string, List<GitCommit>> LoadCommits(Docset docset)
        {
            using (Log.Measure("Loading git commits"))
            {
                var repoRoot = GitUtility.FindRepo(Path.GetFullPath(docset.DocsetPath));
                var files = docset.BuildScope
                    .Where(d => d.ContentType == ContentType.Markdown || d.ContentType == ContentType.SchemaDocument)
                    .ToList();
                var filesFromRepoRoot = files
                    .Select(d => PathUtility.NormalizeFile(Path.GetRelativePath(repoRoot, Path.GetFullPath(Path.Combine(docset.DocsetPath, d.FilePath)))))
                    .ToList();
                var commitsList = GitUtility.GetCommits(repoRoot, filesFromRepoRoot, Log.Progress);
                var result = new Dictionary<string, List<GitCommit>>();
                for (var i = 0; i < files.Count; i++)
                {
                    result[files[i].FilePath] = commitsList[i];
                }
                return result;
            }
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

        private GitUserInfo ToGitUserInfo(GitUserProfile profile)
        {
            if (profile == null)
                return null;

            return new GitUserInfo
            {
                DisplayName = profile.DisplayName,
                Id = profile.Id,
                ProfileUrl = profile.ProfileUrl,
            };
        }
    }
}
