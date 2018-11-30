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

        private readonly ConcurrentDictionary<string, List<GitCommit>> _contributionCommitsByFile = new ConcurrentDictionary<string, List<GitCommit>>();

        private readonly ConcurrentDictionary<string, List<GitCommit>> _commitsByFile = new ConcurrentDictionary<string, List<GitCommit>>();

        private readonly RepositoryProvider _repositoryProvider;

        private ContributionProvider(GitHubUserCache gitHubUserCache, RepositoryProvider repositoryProvider)
        {
            Debug.Assert(repositoryProvider != null);

            _gitHubUserCache = gitHubUserCache;
            _repositoryProvider = repositoryProvider;
        }

        public static async Task<ContributionProvider> Create(Docset docset, GitHubUserCache cache, GitCommitProvider gitCommitProvider, RepositoryProvider repositoryProvider)
        {
            var result = new ContributionProvider(cache, repositoryProvider);
            await result.LoadCommits(docset, gitCommitProvider);
            await result.LoadCommitsTime(docset);
            return result;
        }

        public async Task<(List<Error> error, Contributor author, List<Contributor> contributors, DateTime updatedAt)> GetAuthorAndContributors(
            Document document,
            string authorName)
        {
            Debug.Assert(document != null);
            var (repo, _) = _repositoryProvider.GetRepository(document);
            if (repo == null)
            {
                return default;
            }

            var contributionCommits = _contributionCommitsByFile.TryGetValue(document.FilePath, out var cc) ? cc : default;
            var commits = _commitsByFile.TryGetValue(document.FilePath, out var fc) ? fc : default;

            var excludes = document.Docset.Config.Contribution.ExcludedContributors;

            var contributors = new List<Contributor>();
            var errors = new List<Error>();
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var userIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var updatedDateTime = GetUpdatedAt(document, commits);

            var resolveGitHubUsers = GitHubUtility.TryParse(repo?.Remote, out var gitHubOwner, out var gitHubRepoName) && document.Docset.Config.GitHub.ResolveUsers;

            // Resolve contributors from commits
            if (contributionCommits != null)
            {
                foreach (var commit in contributionCommits)
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
                    for (var i = contributionCommits.Count - 1; i >= 0; i--)
                    {
                        var user = await GetContributor(contributionCommits[i]);
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

            var (repo, pathToRepo) = _repositoryProvider.GetRepository(document);
            if (repo == null)
                return default;

            var repoHost = GitHubUtility.TryParse(repo.Remote, out _, out _) ? GitHost.GitHub : GitHost.Unknown;
            var commit = _commitsByFile.TryGetValue(document.FilePath, out var value) && value.Count > 0
                ? value[0].Sha
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

                var (editRemote, editBranch) = (repo.Remote, repo.Branch);
                if (!string.IsNullOrEmpty(document.Docset.Config.Contribution.Repository))
                {
                    (editRemote, editBranch) = HrefUtility.SplitGitHref(document.Docset.Config.Contribution.Repository);
                    if (document.Docset.IsLocalized())
                    {
                        (editRemote, _) = LocalizationConvention.GetLocalizationRepo(
                                            document.Docset.Config.Localization.Mapping,
                                            document.Docset.Config.Localization.Bilingual,
                                            editRemote,
                                            editBranch,
                                            document.Docset.Locale,
                                            document.Docset.Config.Localization.DefaultLocale);
                    }
                }

                // git edit url, only works for github repo
                if (GitHubUtility.TryParse(editRemote, out _, out _))
                {
                    return $"{editRemote}/blob/{editBranch}/{pathToRepo}";
                }

                return null;
            }
        }

        private async Task LoadCommitsTime(Docset docset)
        {
            if (!string.IsNullOrEmpty(docset.Config.Contribution.GitCommitsTime))
            {
                var path = docset.GetFileRestorePath(docset.Config.Contribution.GitCommitsTime);
                var content = await ProcessUtility.ReadFile(path);

                foreach (var commit in JsonUtility.Deserialize<GitCommitsTime>(content).Commits)
                {
                    _updateTimeByCommit.Add(commit.Sha, commit.BuiltAt);
                }
            }
        }

        private async Task LoadCommits(Docset docset, GitCommitProvider commitProvider)
        {
            var errors = new List<Error>();

            if (docset.Config.Contribution.ShowContributors)
            {
                var bilingual = docset.IsLocalized() && docset.Config.Localization.Bilingual;
                var filesByRepo =
                    from file in docset.BuildScope
                    where file.ContentType == ContentType.Page
                    let fileInRepo = _repositoryProvider.GetRepository(file)
                    where fileInRepo.repo != null
                    group (file, fileInRepo.pathToRepo)
                    by fileInRepo.repo;

                foreach (var group in filesByRepo)
                {
                    var repo = group.Key;
                    var repoPath = repo.Path;
                    var contributionBranch = bilingual && LocalizationConvention.TryGetContributionBranch(repo.Branch, out var cBranch) ? cBranch : null;

                    using (Progress.Start($"Loading commits for '{repoPath}'"))
                    {
                        await ParallelUtility.ForEach(
                            group,
                            async pair =>
                            {
                                _commitsByFile[pair.file.FilePath] = await commitProvider.GetCommitHistory(repo, pair.pathToRepo);
                                if (!string.IsNullOrEmpty(contributionBranch))
                                {
                                    _contributionCommitsByFile[pair.file.FilePath] = await commitProvider.GetCommitHistory(repo, pair.pathToRepo, contributionBranch);
                                }
                                else
                                {
                                    _contributionCommitsByFile[pair.file.FilePath] = _commitsByFile[pair.file.FilePath];
                                }
                            },
                            Progress.Update);
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
