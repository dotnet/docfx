// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class ContributionInfo
    {
        private static readonly string s_defaultProfilePath = Path.Combine(AppData.CacheDir, "user-profile.json");
        private static readonly ContributionInfo s_empty = new ContributionInfo(
            new Dictionary<string, List<GitCommit>>(),
            new Dictionary<string, DateTime>(),
            UserProfileCache.Create(s_defaultProfilePath));

        private readonly UserProfileCache _userProfileCache;

        private readonly IReadOnlyDictionary<string, List<GitCommit>> _commitsByFile;

        private readonly IReadOnlyDictionary<string, DateTime> _updateTimeByCommit;

        private ContributionInfo(
            IReadOnlyDictionary<string, List<GitCommit>> commitsByFile,
            IReadOnlyDictionary<string, DateTime> updateTimeByCommit,
            UserProfileCache userProfileCache)
        {
            _commitsByFile = commitsByFile;
            _updateTimeByCommit = updateTimeByCommit;
            _userProfileCache = userProfileCache;
        }

        public static ContributionInfo Load(Docset docset)
        {
            if (docset.Repository == null)
            {
                return s_empty;
            }

            var commitsByFile = LoadCommits(docset);

            var updateTimeByCommit = string.IsNullOrEmpty(docset.Config.Contribution.GitCommitsTime)
                ? new Dictionary<string, DateTime>()
                : GitCommitsTime.Create(GetFileFromConfig(docset, docset.Config.Contribution.GitCommitsTime, docset.RestoreMap)).ToDictionary();

            var userProfileCachePath = string.IsNullOrEmpty(docset.Config.Contribution.UserProfileCache)
                ? s_defaultProfilePath
                : GetFileFromConfig(docset, docset.Config.Contribution.UserProfileCache, docset.RestoreMap);

            return new ContributionInfo(commitsByFile, updateTimeByCommit, UserProfileCache.Create(userProfileCachePath));
        }

        private static string GetFileFromConfig(Docset docset, string path, RestoreMap restoreMap)
        {
            if (!HrefUtility.IsAbsoluteHref(path))
            {
                // directly return the relative path
                return Path.Combine(docset.DocsetPath, path);
            }

            return restoreMap.GetUrlRestorePath(path);
        }

        private static IReadOnlyDictionary<string, List<GitCommit>> LoadCommits(Docset docset)
        {
            Debug.Assert(docset.Repository != null);

            var repoRoot = docset.Repository.RepositoryPath;

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

        public (List<Error> errors, GitUserInfo author, GitUserInfo[] contributors, DateTime updatedAt) GetContributorInfo(
            Document document,
            string author,
            DateTime? updateDate)
        {
            Debug.Assert(document != null);

            var errors = new List<Error>();
            UserProfile authorInfo = null;
            if (!string.IsNullOrEmpty(author))
            {
                authorInfo = _userProfileCache.GetByUserName(author);
                if (authorInfo == null)
                    errors.Add(Errors.AuthorNotFound(author));
            }
            var contributors = new List<UserProfile>();

            if (_commitsByFile.TryGetValue(document.FilePath, out var commits) && commits.Count != 0)
            {
                if (string.IsNullOrEmpty(author))
                {
                    for (var i = commits.Count - 1; i >= 0; i--)
                    {
                        if (!string.IsNullOrEmpty(commits[i].AuthorEmail))
                        {
                            authorInfo = _userProfileCache.GetByUserEmail(commits[i].AuthorEmail);
                            if (authorInfo != null)
                                break;
                        }
                    }
                }

                contributors = (from commit in commits
                                where !string.IsNullOrEmpty(commit.AuthorEmail)
                                let info = _userProfileCache.GetByUserEmail(commit.AuthorEmail)
                                where info != null && !(authorInfo != null && info.Id == authorInfo.Id)
                                group info by info.Id into g
                                select g.First()).ToList();
            }

            DateTime updateDateTime;
            if (updateDate != null)
            {
                updateDateTime = updateDate.Value;
            }
            else if (commits?.Count > 0)
            {
                if (_updateTimeByCommit.TryGetValue(commits[0].Sha, out var timeFromHistory))
                    updateDateTime = timeFromHistory;
                else
                    updateDateTime = commits[0].Time.UtcDateTime;
            }
            else
            {
                updateDateTime = File.GetLastWriteTimeUtc(Path.Combine(document.Docset.DocsetPath, document.FilePath));
            }

            return (errors, ToGitUserInfo(authorInfo), contributors.Select(ToGitUserInfo).ToArray(), updateDateTime);
        }

        public (string editUrl, string contentUrl, string commitUrl) GetGitUrls(Document document)
        {
            Debug.Assert(document != null);

            if (document.Docset.Repository == null)
            {
                return default;
            }

            var repoPath = document.Docset.Repository.RepositoryPath;
            var fullPath = Path.GetFullPath(Path.Combine(document.Docset.DocsetPath, document.FilePath));
            var relativePath = PathUtility.NormalizeFile(Path.GetRelativePath(repoPath, fullPath));

            var editBranch = document.Docset.Config.Contribution.Branch ?? "{branch}";
            var editRepo = document.Docset.Config.Contribution.Repository ?? "{repo}";

            var editUrl = document.Docset.Config.Contribution.Enabled
                ? $"https://github.com/{editRepo}/blob/{editBranch}/{relativePath}"
                : null;

            var commitUrl = _commitsByFile.TryGetValue(document.FilePath, out var commits) && commits.Count > 0
                ? $"https://github.com/{{repo}}/blob/{commits[0].Sha}/{relativePath}"
                : null;

            var contentUrl = $"https://github.com/{{repo}}/blob/{{branch}}/{relativePath}";

            return (editUrl, contentUrl, commitUrl);
        }

        private GitUserInfo ToGitUserInfo(UserProfile profile)
        {
            if (profile == null)
                return null;

            return new GitUserInfo
            {
                Name = profile.Name,
                DisplayName = profile.DisplayName,
                Id = profile.Id,
                ProfileUrl = profile.ProfileUrl,
            };
        }
    }
}
