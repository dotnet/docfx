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
    internal class ContributionInfo
    {
        private static readonly string s_branchName =
            Environment.GetEnvironmentVariable("DOCFX_BRANCH") ??
            Environment.GetEnvironmentVariable("TRAVIS_BRANCH") ?? /* https://docs.travis-ci.com/user/environment-variables/ */
            Environment.GetEnvironmentVariable("APPVEYOR_REPO_BRANCH") ?? /* https://www.appveyor.com/docs/environment-variables/ */
            Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME") ?? /* https://docs.microsoft.com/en-us/vsts/pipelines/build/variables */
            Environment.GetEnvironmentVariable("CI_COMMIT_REF_NAME") ?? /* https://docs.gitlab.com/ce/ci/variables/README.html */
            Environment.GetEnvironmentVariable("GIT_LOCAL_BRANCH"); /* https://wiki.jenkins.io/display/JENKINS/Git+Plugin */

        private static readonly string s_defaultProfilePath = Path.Combine(AppData.CacheDir, "user-profile.json");

        private readonly ConcurrentDictionary<string, GitRepoInfo> _folderRepoInfocache = new ConcurrentDictionary<string, GitRepoInfo>();

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

            var repoInfo = GetGitRepoInfo(document);
            if (repoInfo?.Host != GitHost.GitHub)
                return default;

            var fullPath = Path.GetFullPath(Path.Combine(document.Docset.DocsetPath, document.FilePath));
            var relativePath = PathUtility.NormalizeFile(Path.GetRelativePath(repoInfo.RootPath, fullPath));

            var contentBranch = s_branchName ?? repoInfo.Branch ?? "master";

            var editBranch = string.IsNullOrEmpty(document.Docset.Config.Contribution.Branch)
                ? contentBranch
                : document.Docset.Config.Contribution.Branch;

            var editRepo = string.IsNullOrEmpty(document.Docset.Config.Contribution.Repository)
                ? $"https://github.com/{repoInfo.Account}/{repoInfo.Name}"
                : document.Docset.Config.Contribution.Repository;

            var editUrl = document.Docset.Config.Contribution.Enabled ? $"{editRepo}/blob/{editBranch}/{relativePath}" : null;
            var contentUrl = $"https://github.com/{repoInfo.Account}/{repoInfo.Name}/blob/{contentBranch}/{relativePath}";
            var commitUrl = _commitsByFile.TryGetValue(document.FilePath, out var commits) && commits.Count > 0
                ? $"https://github.com/{repoInfo.Account}/{repoInfo.Name}/blob/{commits[0].Sha}/{relativePath}"
                : null;

            return (editUrl, contentUrl, commitUrl);
        }

        private GitRepoInfo GetGitRepoInfo(Document document)
        {
            Debug.Assert(document != null);

            return GetFolderGitRepoInfo(Path.GetDirectoryName(Path.Combine(document.Docset.DocsetPath, document.FilePath)));
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
            var parent = folder.Substring(0, folder.LastIndexOf("/"));
            return Directory.Exists(parent)
                ? _folderRepoInfocache.GetOrAdd(parent, GetFolderGitRepoInfoCore)
                : null;
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
