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

        private readonly GitHubUserCache _gitHubUserCache;

        private readonly IReadOnlyDictionary<string, DateTime> _updateTimeByCommit;

        private readonly ConcurrentDictionary<string, Repository> _repositoryByFolder = new ConcurrentDictionary<string, Repository>();

        private readonly IReadOnlyDictionary<string, (Repository, List<GitCommit> commits)> _commitsByFile;

        public ContributionInfo(Docset docset, string gitHubToken)
        {
            _commitsByFile = LoadCommits(docset);

            _updateTimeByCommit = string.IsNullOrEmpty(docset.Config.Contribution.GitCommitsTime)
                ? new Dictionary<string, DateTime>()
                : FileUtility.ReadJsonFile<GitCommitsTime>(
                    docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, docset.Config.Contribution.GitCommitsTime)).ToDictionary();

            _gitHubUserCache = new GitHubUserCache(gitHubToken);

            if (File.Exists(s_defaultProfilePath))
            {
                _gitHubUserCache.Update(FileUtility.ReadJsonFile<GitHubUser[]>(s_defaultProfilePath));
            }

            if (!string.IsNullOrEmpty(docset.Config.Contribution.UserProfileCache))
            {
                _gitHubUserCache.Update(FileUtility.ReadJsonFile<GitHubUser[]>(
                    docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, docset.Config.Contribution.UserProfileCache)));
            }
        }

        public async Task<(List<Error> error, Contributor author, Contributor[] contributors, DateTime updatedAt)> GetContributorInfo(
            Document document,
            string author)
        {
            Debug.Assert(document != null);

            var (repo, commits) = _commitsByFile.TryGetValue(document.FilePath, out var value) ? value : default;
            var (error, authorInfo) = repo.Host == GitHost.GitHub ? await GetAuthor(document, author, repo, commits) : default;
            var (errors, contributors) = repo.Host == GitHost.GitHub ? await GetContributors(document, authorInfo, repo, commits) : default;
            var updatedDateTime = GetUpdatedAt(document, commits);

            errors.Add(error);
            return (errors, ToContributor(authorInfo), contributors.Select(ToContributor).ToArray(), updatedDateTime);
        }

        public (string editUrl, string contentUrl, string commitUrl) GetGitUrls(Document document)
        {
            Debug.Assert(document != null);

            var (repo, pathToRepo, _) = GetRepository(document);
            if (repo == null)
                return default;

            var branch = repo.Branch ?? "master";
            var editRepo = document.Docset.Config.Contribution.Repository ?? $"{repo.Owner}/{repo.Name}";
            var editBranch = document.Docset.Config.Contribution.Branch ?? branch;

            var editUrl = document.Docset.Config.Contribution.ShowEdit
                ? $"https://github.com/{editRepo}/blob/{editBranch}/{pathToRepo}"
                : null;

            var commit = _commitsByFile.TryGetValue(document.FilePath, out var value) && value.commits.Count > 0
                ? value.commits[0].Sha
                : repo.Commit;

            var commitUrl = commit != null ? $"https://github.com/{repo.Owner}/{repo.Name}/blob/{commit}/{pathToRepo}" : null;
            var contentUrl = $"https://github.com/{repo.Owner}/{repo.Name}/blob/{branch}/{pathToRepo}";

            return (editUrl, contentUrl, commitUrl);
        }

        private async Task<(Error error, GitHubUser author)> GetAuthor(Document doc, string authorName, Repository repo, List<GitCommit> fileCommits)
        {
            var excludes = doc.Docset.Config.Contribution.ExcludedContributors;

            if (!string.IsNullOrEmpty(authorName))
            {
                return await _gitHubUserCache.GetByLogin(authorName);
            }

            if (fileCommits != null && fileCommits.Count != 0)
            {
                for (var i = fileCommits.Count - 1; i >= 0; i--)
                {
                    var (error, user) = await _gitHubUserCache.GetByCommit(fileCommits[i].AuthorEmail, repo.Owner, repo.Name, fileCommits[i].Sha);

                    if (user != null && !excludes.Contains(authorName))
                    {
                        return (null, user);
                    }
                }
            }

            return (Errors.GitHubUserNotFound(authorName), null);
        }

        private async Task<(List<Error>, List<GitHubUser>)> GetContributors(Document doc, GitHubUser author, Repository repo, List<GitCommit> fileCommits)
        {
            var users = new List<GitHubUser>();
            var errors = new List<Error>();
            var logins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var excludes = doc.Docset.Config.Contribution.ExcludedContributors;

            foreach (var commit in fileCommits)
            {
                if (emails.Add(commit.AuthorEmail))
                {
                    var (error, user) = await _gitHubUserCache.GetByCommit(commit.AuthorEmail, repo.Owner, repo.Name, commit.Sha);
                    if (error != null)
                        errors.Add(error);

                    if (user != null && user.Id != author.Id && !excludes.Contains(user.Login) && logins.Add(user.Login))
                        users.Add(user);
                }
            }

            return (errors, users);
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
                return default;

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

        private Contributor ToContributor(GitHubUser user)
        {
            if (user == null)
                return null;

            return new Contributor
            {
                Id = user.Id.ToString(),
                Name = user.Login,
                DisplayName = user.Name,
                ProfileUrl = "https://github.com/" + user.Login,
            };
        }

        private Dictionary<string, (Repository, List<GitCommit> commits)> LoadCommits(Docset docset)
        {
            var result = new Dictionary<string, (Repository, List<GitCommit> commits)>();

            if (docset.Config.Contribution.ShowContributors)
            {
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
                    var repo = group.Key;
                    var repoPath = repo.RepositoryPath;
                    var (commitsByFile, allCommits) = GitUtility.GetCommits(repoPath, pathToRepo);
                    for (var i = 0; i < pathToDocset.Count; i++)
                    {
                        result.Add(pathToDocset[i], (group.Key, commitsByFile[i]));
                    }
                }
            }

            return result;
        }
    }
}
