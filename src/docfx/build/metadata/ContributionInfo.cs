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

        private readonly IReadOnlyDictionary<string, DateTime> _updateTimeByCommit;

        private readonly ConcurrentDictionary<string, Repository> _repositoryByFolder = new ConcurrentDictionary<string, Repository>();

        private readonly GitHubAccessor _github;

        private IReadOnlyDictionary<string, List<GitCommit>> _commitsByFile;

        public static async Task<ContributionInfo> Load(Docset docset, string gitToken)
        {
            var result = new ContributionInfo(docset, gitToken);
            await result.LoadCommits(docset);
            return result;
        }

        public async Task<(List<Error> errors, GitUserInfo author, GitUserInfo[] contributors, DateTime updatedAt)> GetContributorInfo(
            Document document,
            string author,
            DateTime? updateDate)
        {
            Debug.Assert(document != null);

            var errors = new List<Error>();
            UserProfile authorInfo = null;
            if (!string.IsNullOrEmpty(author))
            {
                try
                {
                    authorInfo = await _userProfileCache.GetByUserName(author);
                }
                catch (DocfxInternalException ex)
                {
                    errors.Add(Errors.AuthorNotFound(author, ex));
                }
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
                                where info != null && !ContributorIsAuthor(info, authorInfo)
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

            bool ContributorIsAuthor(UserProfile contributorProfile, UserProfile authorProfile)
                => contributorProfile.Id == authorProfile?.Id;
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
            var editRepo = document.Docset.Config.Contribution.Repository ?? repo.FullName;
            var editBranch = document.Docset.Config.Contribution.Branch ?? branch;

            var editUrl = document.Docset.Config.Contribution.Enabled
                ? $"https://github.com/{editRepo}/blob/{editBranch}/{pathToRepo}"
                : null;

            var commitUrl = _commitsByFile.TryGetValue(document.FilePath, out var commits) && commits.Count > 0
                ? $"https://github.com/{repo.FullName}/blob/{commits[0].Sha}/{pathToRepo}"
                : null;

            var contentUrl = $"https://github.com/{repo.FullName}/blob/{branch}/{pathToRepo}";

            return (editUrl, contentUrl, commitUrl);
        }

        private ContributionInfo(Docset docset, string gitToken)
        {
            _github = new GitHubAccessor(gitToken);

            _updateTimeByCommit = string.IsNullOrEmpty(docset.Config.Contribution.GitCommitsTime)
                ? new Dictionary<string, DateTime>()
                : GitCommitsTime.Create(docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, docset.Config.Contribution.GitCommitsTime)).ToDictionary();

            var userProfilePath = string.IsNullOrEmpty(docset.Config.Contribution.UserProfileCache)
                    ? s_defaultProfilePath
                    : docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, docset.Config.Contribution.UserProfileCache);
            _userProfileCache = UserProfileCache.Create(userProfilePath, _github);
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

        private async Task LoadCommits(Docset docset)
        {
            var result = new Dictionary<string, List<GitCommit>>();

            // TODO: report errors
            var errors = new List<Error>();

            var filesByRepo =
                from file in docset.BuildScope
                where file.ContentType == ContentType.Markdown || file.ContentType == ContentType.SchemaDocument
                let fileInRepo = GetRepository(file)
                where fileInRepo.repo != null
                group (file, fileInRepo.pathToRepo) by fileInRepo.repo;

            foreach (var group in filesByRepo)
            {
                var pathToDocset = group.Select(pair => pair.file.FilePath).ToList();
                var pathToRepo = group.Select(pair => pair.pathToRepo).ToList();
                var repo = group.Key;
                var repoPath = repo.RepositoryPath;
                var (commitsByFile, allCommits) = GitUtility.GetCommits(repoPath, pathToRepo);
                for (var i = 0; i < pathToDocset.Count; i++)
                {
                    result.Add(pathToDocset[i], commitsByFile[i]);
                }
                foreach (var commit in allCommits)
                {
                    try
                    {
                        await UpdateCache(commit, repo);
                    }
                    catch (DocfxInternalException ex)
                    {
                        errors.Add(Errors.CommitInfoNotFound(commit.Sha, ex));
                    }
                }
            }

            _commitsByFile = result;
        }

        private async Task UpdateCache(GitCommit commit, Repository repo)
        {
            var author = commit.AuthorEmail;
            if (string.IsNullOrEmpty(author) || _userProfileCache.GetByUserEmail(author) != null)
                return;

            var authorName = await _github.GetNameByCommit(repo.Owner, repo.Name, commit.Sha);
            var profile = (await _github.GetUserProfileByName(authorName)).AddEmail(author);
            _userProfileCache.AddOrUpdate(authorName, profile, (k, v) => v.AddEmail(author));
        }
    }
}
