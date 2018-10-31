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
    internal class ContributionProvider
    {
        private readonly GitHubUserCache _gitHubUserCache;

        private readonly Dictionary<string, DateTime> _updateTimeByCommit = new Dictionary<string, DateTime>();

        private readonly ConcurrentDictionary<string, Repository> _repositoryByFolder = new ConcurrentDictionary<string, Repository>();

        private readonly ConcurrentDictionary<string, (Repository, List<GitCommit> commits)> _commitsByFile = new ConcurrentDictionary<string, (Repository, List<GitCommit> commits)>();

        private ContributionProvider(GitHubUserCache gitHubUserCache)
        {
            _gitHubUserCache = gitHubUserCache;
        }

        public static async Task<ContributionProvider> Create(Docset docset, GitHubUserCache cache)
        {
            var result = new ContributionProvider(cache);
            await result.LoadCommits(docset);
            await result.LoadCommitsTime(docset);
            return result;
        }

        public async Task<(List<Error> error, Contributor author, List<Contributor> contributors, DateTime updatedAt)> GetAuthorAndContributors(
            Document document,
            string authorName)
        {
            Debug.Assert(document != null);

            var (repo, commits) = _commitsByFile.TryGetValue(document.FilePath, out var value) ? value : default;
            var excludes = document.Docset.Config.Contribution.ExcludedContributors;

            var contributors = new List<Contributor>();
            var errors = new List<Error>();
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var userIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var updatedDateTime = GetUpdatedAt(document, commits);

            var resolveGitHubUsers = GitHubUtility.TryParse(repo?.Remote, out var gitHubOwner, out var gitHubRepoName) && document.Docset.Config.GitHub.ResolveUsers;

            // Resolve contributors from commits
            if (commits != null)
            {
                foreach (var commit in commits)
                {
                    if (!emails.Add(commit.AuthorEmail))
                        continue;

                    var contributor = await GetContributor(commit);
                    if (contributor != null && !excludes.Contains(contributor.Name) && userIds.Add(contributor.Id))
                    {
                        contributors.Add(contributor);
                    }
                }
            }

            var author = await GetAuthor();
            if (author != null)
            {
                contributors.RemoveAll(c => c.Id == author.Id);
            }

            return (errors, author, contributors, updatedDateTime);

            async Task<Contributor> GetContributor(GitCommit commit)
            {
                if (!resolveGitHubUsers)
                {
                    return new Contributor { DisplayName = commit.AuthorName, Id = commit.AuthorEmail };
                }

                var (error, user) = await _gitHubUserCache.GetByCommit(commit.AuthorEmail, gitHubOwner, gitHubRepoName, commit.Sha);
                errors.AddIfNotNull(error);

                return user?.ToContributor();
            }

            async Task<Contributor> GetAuthor()
            {
                if (!string.IsNullOrEmpty(authorName))
                {
                    if (resolveGitHubUsers && !excludes.Contains(authorName))
                    {
                        // Remove author from contributors if author name is specified
                        var (error, result) = await _gitHubUserCache.GetByLogin(authorName);
                        errors.AddIfNotNull(error);
                        return result?.ToContributor();
                    }
                }
                else if (contributors.Count > 0)
                {
                    // When author name is not specified, last contributor is author
                    for (var i = commits.Count - 1; i >= 0; i--)
                    {
                        var user = await GetContributor(commits[i]);
                        if (user != null)
                        {
                            return user;
                        }
                    }
                }
                return null;
            }
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

            var repoHost = GitHubUtility.TryParse(repo.Remote, out _, out _) ? GitHost.GitHub : GitHost.Unknown;
            var commit = _commitsByFile.TryGetValue(document.FilePath, out var value) && value.commits.Count > 0
                ? value.commits[0].Sha
                : repo.Commit;

            return (GetEditUrl(), GetContentUrl(), GetCommitUrl());

            string GetCommitUrl()
            {
                switch (repoHost)
                {
                    case GitHost.GitHub:
                        return commit != null ? $"{repo.Remote}/blob/{commit}/{pathToRepo}" : null;
                    default:
                        return null;
                }
            }

            string GetContentUrl()
            {
                switch (repoHost)
                {
                    case GitHost.GitHub:
                        return $"{repo.Remote}/blob/{repo.Branch}/{pathToRepo}";
                    default:
                        return null;
                }
            }

            string GetEditUrl()
            {
                if (!document.Docset.Config.Contribution.ShowEdit)
                {
                    return null;
                }

                // only loc docset document can be built, so the current repo is loc repo
                var (locRemote, locBranch) = (repo.Remote, repo.Branch);
                if (!string.IsNullOrEmpty(document.Docset.Config.Contribution.Repository))
                {
                    (locRemote, locBranch) = GitUtility.GetGitRemoteInfo(document.Docset.Config.Contribution.Repository);
                    (locRemote, _) = LocalizationConvention.GetLocalizationRepo(
                        document.Docset.Config.Localization.Mapping,
                        document.Docset.Config.Localization.Bilingual,
                        locRemote,
                        locBranch,
                        document.Docset.Locale,
                        document.Docset.Config.Localization.DefaultLocale);
                }

                // git edit url, only works for github repo
                if (GitHubUtility.TryParse(locRemote, out _, out _))
                {
                    return $"{locRemote}/blob/{locBranch}/{pathToRepo}";
                }

                return null;
            }
        }

        private (Repository repo, string pathToRepo, bool isDocsetRepo) GetRepository(Document document)
        {
            var fullPath = PathUtility.NormalizeFile(Path.Combine(document.Docset.DocsetPath, document.FilePath));
            var repo = GetRepository(fullPath);
            if (repo == null)
                return default;

            var isDocsetRepo = document.Docset.DocsetPath.StartsWith(repo.Path, PathUtility.PathComparison);
            return (repo, PathUtility.NormalizeFile(Path.GetRelativePath(repo.Path, fullPath)), isDocsetRepo);
        }

        private Repository GetRepository(string path)
        {
            if (GitUtility.IsRepo(path))
                return Repository.CreateFromFolder(path);

            var parent = path.Substring(0, path.LastIndexOf("/"));
            return Directory.Exists(parent)
                ? _repositoryByFolder.GetOrAdd(parent, GetRepository)
                : null;
        }

        private async Task LoadCommitsTime(Docset docset)
        {
            if (!string.IsNullOrEmpty(docset.Config.Contribution.GitCommitsTime))
            {
                var path = docset.RestoreMap.GetUrlRestorePath(docset.Config.Contribution.GitCommitsTime);
                var content = await ProcessUtility.ReadFile(path);

                foreach (var commit in JsonUtility.Deserialize<GitCommitsTime>(content).Item2.Commits)
                {
                    _updateTimeByCommit.Add(commit.Sha, commit.BuiltAt);
                }
            }
        }

        private async Task LoadCommits(Docset docset)
        {
            var errors = new List<Error>();

            if (docset.Config.Contribution.ShowContributors)
            {
                var filesByRepo =
                    from file in docset.BuildScope
                    where file.ContentType == ContentType.Page
                    let fileInRepo = GetRepository(file)
                    where fileInRepo.repo != null
                    group (file, fileInRepo.pathToRepo)
                    by fileInRepo.repo;

                foreach (var group in filesByRepo)
                {
                    var repo = group.Key;
                    var repoPath = repo.Path;
                    var commitCachePath = Path.Combine(AppData.CacheDir, "commits", HashUtility.GetMd5Hash(repo.Remote));

                    using (Progress.Start($"Loading commits for '{repoPath}'"))
                    using (var commitsProvider = await GitCommitProvider.Create(repoPath, commitCachePath))
                    {
                        ParallelUtility.ForEach(
                            group,
                            pair => _commitsByFile[pair.file.FilePath] = (group.Key, commitsProvider.GetCommitHistory(pair.pathToRepo)),
                            Progress.Update);

                        await commitsProvider.SaveCache();
                    }
                }
            }
        }

        private enum GitHost
        {
            Unknown,
            GitHub,
        }
    }
}
