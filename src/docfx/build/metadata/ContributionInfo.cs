// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class ContributionInfo
    {
        private static readonly string s_defaultProfilePath = Path.Combine(AppData.CacheDir, "user-profile.json");

        private readonly UserProfileCache _userProfileCache;

        private readonly IReadOnlyDictionary<string, List<GitCommit>> _commitsByFile;

        private readonly IReadOnlyDictionary<string, DateTime> _updateTimeByCommit;

        private readonly ConcurrentDictionary<string, Repository> _repositoryByFolder = new ConcurrentDictionary<string, Repository>();

        private readonly GitHubAccessor _github;

        private ContributionInfo(Docset docset, string gitToken)
        {
            _commitsByFile = LoadCommits(docset);
            _github = new GitHubAccessor(gitToken);

            _updateTimeByCommit = string.IsNullOrEmpty(docset.Config.Contribution.GitCommitsTime)
                ? new Dictionary<string, DateTime>()
                : GitCommitsTime.Create(docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, docset.Config.Contribution.GitCommitsTime)).ToDictionary();

            var userProfilePath = string.IsNullOrEmpty(docset.Config.Contribution.UserProfileCache)
                    ? s_defaultProfilePath
                    : docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, docset.Config.Contribution.UserProfileCache);
            _userProfileCache = UserProfileCache.Create(userProfilePath, _github);
        }

        public static ContributionInfo Load(Docset docset, string gitToken)
        {
            return new ContributionInfo(docset, gitToken);
        }

        public async Task<(List<Error> errors, GitUserInfo author, GitUserInfo[] contributors, DateTime updatedAt)> GetContributorInfo(
            Document document,
            string author)
        {
            Debug.Assert(document != null);

            _commitsByFile.TryGetValue(document.FilePath, out var commits);
            var (errors, authorInfo) = await GetAuthor(document, author, commits);
            var contributors = GetContributors(document, authorInfo, commits);
            var updatedDateTime = GetUpdatedAt(document, commits);

            return (errors, ToGitUserInfo(authorInfo), contributors.Select(ToGitUserInfo).ToArray(), updatedDateTime);
        }

        public (string editUrl, string contentUrl, string commitUrl) GetGitUrls(Document document)
        {
            Debug.Assert(document != null);

            var (repo, pathToRepo, _) = GetRepository(document);
            if (repo == null)
            {
                return default;
            }

            var branch = repo.Branch ?? "master";
            var editRepo = document.Docset.Config.Contribution.Repository ?? repo.Name;
            var editBranch = document.Docset.Config.Contribution.Branch ?? branch;

            var editUrl = document.Docset.Config.Contribution.ShowEdit
                ? $"https://github.com/{editRepo}/blob/{editBranch}/{pathToRepo}"
                : null;

            var commit = _commitsByFile.TryGetValue(document.FilePath, out var commits) && commits.Count > 0
                ? commits[0].Sha
                : repo.Commit;

            var commitUrl = commit != null ? $"https://github.com/{repo.Name}/blob/{commit}/{pathToRepo}" : null;
            var contentUrl = $"https://github.com/{repo.Name}/blob/{branch}/{pathToRepo}";

            return (editUrl, contentUrl, commitUrl);
        }

        private async Task<(List<Error> errors, UserProfile author)> GetAuthor(Document doc, string authorName, List<GitCommit> fileCommits)
        {
            UserProfile authorProfile = null;
            var errors = new List<Error>();
            if (!string.IsNullOrEmpty(authorName) && !doc.Docset.Config.Contribution.ExcludedContributors.Contains(authorName))
            {
                try
                {
                    authorProfile = await _userProfileCache.GetByUserName(authorName);
                }
                catch (DocfxException ex)
                {
                    errors.Add(Errors.AuthorNotFound(authorName, ex));
                }
            }

            if (fileCommits != null && fileCommits.Count != 0)
            {
                if (string.IsNullOrEmpty(authorName))
                {
                    for (var i = fileCommits.Count - 1; i >= 0; i--)
                    {
                        if (!string.IsNullOrEmpty(fileCommits[i].AuthorEmail))
                        {
                            authorProfile = _userProfileCache.GetByUserEmail(fileCommits[i].AuthorEmail);
                            if (authorProfile != null && !doc.Docset.Config.Contribution.ExcludedContributors.Contains(authorName))
                                break;
                        }
                    }
                }
            }

            return (errors, authorProfile);
        }

        private List<UserProfile> GetContributors(Document doc, UserProfile authorInfo, List<GitCommit> fileCommits)
        {
            if (fileCommits == null || fileCommits.Count == 0)
            {
                return new List<UserProfile>();
            }

            return (from commit in fileCommits
                    where !string.IsNullOrEmpty(commit.AuthorEmail)
                    let info = _userProfileCache.GetByUserEmail(commit.AuthorEmail)
                    where info != null && !(authorInfo != null && info.Id == authorInfo.Id) && !doc.Docset.Config.Contribution.ExcludedContributors.Contains(info.Name)
                    group info by info.Id into g
                    select g.First()).ToList();
        }

        private DateTime GetUpdatedAt(Document document, List<GitCommit> fileCommits)
        {
            if (fileCommits?.Count > 0)
            {
                if (_updateTimeByCommit.TryGetValue(fileCommits[0].Sha, out var timeFromHistory))
                    return timeFromHistory;
                else
                    return fileCommits[0].Time.UtcDateTime;
            }
            return File.GetLastWriteTimeUtc(Path.Combine(document.Docset.DocsetPath, document.FilePath));
        }

        private (Repository repo, string pathToRepo, bool isDocsetRepo) GetRepository(Document document)
        {
            var fullPath = PathUtility.NormalizeFile(Path.Combine(document.Docset.DocsetPath, document.FilePath));
            var repo = GetRepository(fullPath);
            if (repo == null)
            {
                return default;
            }

            var isDocsetRepo = document.Docset.DocsetPath.StartsWith(repo.RepositoryPath, PathUtility.PathComparison);
            return (repo, PathUtility.NormalizeFile(Path.GetRelativePath(repo.RepositoryPath, fullPath)), isDocsetRepo);
        }

        private Repository GetRepository(string path)
        {
            if (GitUtility.IsRepo(path))
                return Repository.Create(path);

            var parent = path.Substring(0, path.LastIndexOf("/"));
            return Directory.Exists(parent)
                ? _repositoryByFolder.GetOrAdd(parent, GetRepository)
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

        private IReadOnlyDictionary<string, List<GitCommit>> LoadCommits(Docset docset)
        {
            var result = new Dictionary<string, List<GitCommit>>();

            if (!docset.Config.Contribution.ShowContributors)
            {
                return result;
            }

            var filesByRepo =
                from file in docset.BuildScope
                where file.ContentType == ContentType.Page
                let fileInRepo = GetRepository(file)
                where fileInRepo.repo != null
                group (file, fileInRepo.pathToRepo) by fileInRepo.repo;

            foreach (var group in filesByRepo)
            {
                var pathToDocset = group.Select(pair => pair.file.FilePath).ToList();
                var pathToRepo = group.Select(pair => pair.pathToRepo).ToList();
                var repoPath = group.Key.RepositoryPath;
                var commitsList = GitUtility.GetCommits(repoPath, pathToRepo);
                for (var i = 0; i < pathToDocset.Count; i++)
                {
                    result.Add(pathToDocset[i], commitsList[i]);
                }
            }

            return result;
        }
    }
}
