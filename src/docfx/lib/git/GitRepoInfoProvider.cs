// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

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
            var userProfileCachePath = string.IsNullOrEmpty(docset.Config.Contribution.UserProfileCachePath)
                ? s_defaultCachePath
                : Path.Combine(docset.DocsetPath, docset.Config.Contribution.UserProfileCachePath);
            _githubUserProfileCache = GitUserProfileCache.Create(userProfileCachePath);
        }

        // TODO: add more test cases
        public (List<Error> errors, GitUserInfo author, GitUserInfo[] contributors, DateTime updatedAt) GetContributorInfo(
            Document document,
            string author,
            DateTime? updateDate)
        {
            Debug.Assert(document != null);

            var errors = new List<Error>();
            GitUserProfile authorInfo = null;
            if (!string.IsNullOrEmpty(author))
            {
                authorInfo = _githubUserProfileCache.GetByUserName(author);
                if (authorInfo == null)
                    errors.Add(Errors.AuthorNotFound(author));
            }
            var contributors = new List<GitUserProfile>();

            if (TryGetCommits(document.FilePath, out var commits) && commits.Count != 0)
            {
                if (string.IsNullOrEmpty(author))
                {
                    for (var i = commits.Count - 1; i >= 0; i--)
                    {
                        if (!string.IsNullOrEmpty(commits[i].AuthorEmail))
                        {
                            authorInfo = _githubUserProfileCache.GetByUserEmail(commits[i].AuthorEmail);
                            if (authorInfo != null)
                                break;
                        }
                    }
                }

                contributors = (from commit in commits
                                where !string.IsNullOrEmpty(commit.AuthorEmail)
                                let info = _githubUserProfileCache.GetByUserEmail(commit.AuthorEmail)
                                where info != null
                                group info by info.Id into g
                                select g.First()).ToList();
            }

            if (authorInfo != null && contributors.All(p => p.Id != authorInfo.Id))
                contributors.Add(authorInfo);

            DateTime updateDateTime;
            if (updateDate != null)
            {
                updateDateTime = updateDate.Value;
            }
            else if (commits?.Count > 0)
            {
                // TODO: support read build history
                updateDateTime = commits[0].Time.DateTime;
            }
            else
            {
                updateDateTime = File.GetLastWriteTimeUtc(Path.Combine(document.Docset.DocsetPath, document.FilePath));
            }

            return (errors, ToGitUserInfo(authorInfo), contributors.Select(ToGitUserInfo).ToArray(), updateDateTime);
        }

        public string GetEditLink(Document document)
        {
            Debug.Assert(document != null);

            if (!document.Docset.Config.Contribution.Enabled)
                return null;

            var repoInfo = GetGitRepoInfo(document);
            if (repoInfo?.Host != GitHost.GitHub)
                return null;

            var repo = string.IsNullOrEmpty(document.Docset.Config.Contribution.Repository)
                ? $"https://github.com/{repoInfo.Account}/{repoInfo.Name}"
                : document.Docset.Config.Contribution.Repository;
            var branch = string.IsNullOrEmpty(document.Docset.Config.Contribution.Branch)
                ? repoInfo.Branch ?? "master"
                : document.Docset.Config.Contribution.Branch;
            var fullPath = Path.GetFullPath(Path.Combine(document.Docset.DocsetPath, document.FilePath));
            var relPath = PathUtility.NormalizeFile(Path.GetRelativePath(repoInfo.RootPath, fullPath));

            return $"{repo}/blob/{branch}/{relPath}";
        }

        public bool TryGetCommits(string filePath, out List<GitCommit> commits)
            => _fileCommitsCache.Value.TryGetValue(filePath, out commits);

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
            var repoRoot = GitUtility.FindRepo(Path.GetFullPath(docset.DocsetPath));
            var files = docset.BuildScope
                .Where(d => d.ContentType == ContentType.Markdown || d.ContentType == ContentType.SchemaDocument)
                .ToList();
            var filesFromRepoRoot = files
                .Select(d => PathUtility.NormalizeFile(Path.GetRelativePath(repoRoot, Path.GetFullPath(Path.Combine(docset.DocsetPath, d.FilePath)))))
                .ToList();
            var commitsList = GitUtility.GetCommits(repoRoot, filesFromRepoRoot);
            var result = new Dictionary<string, List<GitCommit>>();
            for (var i = 0; i < files.Count; i++)
            {
                result[files[i].FilePath] = commitsList[i];
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
                return GitRepoInfo.Create(folder);

            // TODO: add GitUtility.Discover repo
            var parent = folder.Substring(0, folder.LastIndexOf("/", StringComparison.Ordinal));
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
