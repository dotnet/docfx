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
        private readonly GitHubUserCache _gitHubUserCache;

        private readonly IReadOnlyDictionary<string, DateTime> _updateTimeByCommit;

        private readonly ConcurrentDictionary<string, Repository> _repositoryByFolder = new ConcurrentDictionary<string, Repository>();

        private readonly IReadOnlyDictionary<string, (Repository, List<GitCommit> commits)> _commitsByFile;

        public ContributionInfo(Docset docset, GitHubUserCache gitHubUserCache)
        {
            _gitHubUserCache = gitHubUserCache;
            _commitsByFile = LoadCommits(docset);
            _updateTimeByCommit = string.IsNullOrEmpty(docset.Config.Contribution.GitCommitsTime)
               ? new Dictionary<string, DateTime>()
               : JsonUtility.ReadJsonFile<GitCommitsTime>(
                   docset.RestoreMap.GetUrlRestorePath(docset.DocsetPath, docset.Config.Contribution.GitCommitsTime)).ToDictionary();
        }

        public async Task<(List<Error> error, Contributor author, List<Contributor> contributors, DateTime updatedAt)> GetAuthorAndContributors(
            Document document,
            string authorName)
        {
            Debug.Assert(document != null);

            var (repo, commits) = _commitsByFile.TryGetValue(document.FilePath, out var value) ? value : default;
            var resolveGitHubUsers = repo?.Host == GitHost.GitHub && document.Docset.Config.GitHub.ResolveUsers;
            var excludes = document.Docset.Config.Contribution.ExcludedContributors;

            var contributors = new List<Contributor>();
            var errors = new List<Error>();
            var logins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var authorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var updatedDateTime = GetUpdatedAt(document, commits);

            // Resolve contributors from commits
            if (commits != null)
            {
                foreach (var commit in commits)
                {
                    if (!emails.Add(commit.AuthorEmail))
                        continue;

                    if (!resolveGitHubUsers && authorNames.Add(commit.AuthorName))
                    {
                        contributors.Add(new Contributor { DisplayName = commit.AuthorName });
                        continue;
                    }

                    var (error, user) = await _gitHubUserCache.GetByCommit(commit.AuthorEmail, repo.Owner, repo.Name, commit.Sha);
                    if (error != null)
                        errors.Add(error);

                    if (user != null && !excludes.Contains(user.Login) && logins.Add(user.Login))
                        contributors.Add(user.ToContributor());
                }
            }

            if (!string.IsNullOrEmpty(authorName))
            {
                if (resolveGitHubUsers)
                {
                    // Remove author from contributors if author name is specified
                    var (error, author) = await _gitHubUserCache.GetByLogin(authorName);
                    if (error != null)
                        errors.Add(error);

                    if (excludes.Contains(authorName))
                        author = null;
                    if (author != null)
                        contributors.RemoveAll(c => c.Id == author.Id.ToString());

                    return (errors, author?.ToContributor(), contributors, updatedDateTime);
                }

                return (errors, new Contributor { DisplayName = authorName }, contributors, updatedDateTime);
            }

            // When author name is not specified, last contributor is author
            if (contributors.Count > 0)
            {
                var author = contributors[contributors.Count - 1];
                contributors.RemoveAt(contributors.Count - 1);
                return (errors, author, contributors, updatedDateTime);
            }

            return (errors, null, contributors, updatedDateTime);
        }

        public DateTime GetUpdatedAt(Document document, List<GitCommit> fileCommits)
        {
            if (fileCommits?.Count > 0)
            {
                return _updateTimeByCommit.TryGetValue(fileCommits[0].Sha, out var timeFromHistory)
                    ? timeFromHistory
                    : fileCommits[0].Time.UtcDateTime;
            }
            return File.GetLastWriteTimeUtc(Path.Combine(document.Docset.DocsetPath, document.FilePath));
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

        private Dictionary<string, (Repository, List<GitCommit> commits)> LoadCommits(Docset docset)
        {
            var errors = new List<Error>();
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
