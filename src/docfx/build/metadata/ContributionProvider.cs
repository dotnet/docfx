// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class ContributionProvider
    {
        private readonly Input _input;
        private readonly Docset _docset;
        private readonly Docset _fallbackDocset;
        private readonly GitHubUserCache _gitHubUserCache;

        // TODO: support CRR and multiple repositories
        private readonly CommitBuildTimeProvider _commitBuildTimeProvider;

        private readonly GitCommitProvider _gitCommitProvider;

        public ContributionProvider(
            Input input, Docset docset, Docset fallbackDocset, GitHubUserCache gitHubUserCache, GitCommitProvider gitCommitProvider)
        {
            _input = input;
            _gitHubUserCache = gitHubUserCache;
            _gitCommitProvider = gitCommitProvider;
            _docset = docset;
            _fallbackDocset = fallbackDocset;
            _commitBuildTimeProvider = docset.Repository != null && docset.Config.UpdateTimeAsCommitBuildTime
                ? new CommitBuildTimeProvider(docset.Repository) : null;
        }

        public async Task<(List<Error> errors, ContributionInfo contributionInfo)> GetContributionInfo(
            Document document, SourceInfo<string> authorName)
        {
            Debug.Assert(document != null);
            var (repo, pathToRepo, commits) = _gitCommitProvider.GetCommitHistory(document);
            if (repo is null)
            {
                return default;
            }

            var contributionCommits = GetContributionCommits();

            var excludes = _docset.Config.GlobalMetadata.ContributorsToExclude.Count > 0
                ? _docset.Config.GlobalMetadata.ContributorsToExclude
                : _docset.Config.Contribution.ExcludeContributors;

            Contributor authorFromCommits = null;
            var contributors = new List<Contributor>();
            var errors = new List<Error>();
            var contributorsGroupByEmail = new Dictionary<string, Contributor>(StringComparer.OrdinalIgnoreCase);
            var contributorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var updatedDateTime = GetUpdatedAt(document, commits);
            var contributionInfo = updatedDateTime != default
                ? new ContributionInfo
                {
                    Contributors = contributors,
                    UpdateAt = updatedDateTime.ToString(
                        document.Docset.Locale == "en-us" ? "M/d/yyyy" : document.Docset.Culture.DateTimeFormat.ShortDatePattern),
                    UpdatedAtDateTime = updatedDateTime,
                }
                : null;

            var isGitHubRepo = UrlUtility.TryParseGitHubUrl(repo?.Remote, out var gitHubOwner, out var gitHubRepoName) ||
                UrlUtility.TryParseGitHubUrl(document.Docset.Config.Contribution.RepositoryUrl, out gitHubOwner, out gitHubRepoName);

            if (!document.Docset.Config.GitHub.ResolveUsers)
            {
                return (errors, contributionInfo);
            }

            // Resolve contributors from commits
            if (contributionCommits != null)
            {
                foreach (var commit in contributionCommits)
                {
                    if (!contributorsGroupByEmail.TryGetValue(commit.AuthorEmail, out var contributor))
                    {
                        contributorsGroupByEmail[commit.AuthorEmail] = contributor = await GetContributor(commit);
                    }

                    if (contributor != null && !excludes.Contains(contributor.Name))
                    {
                        authorFromCommits = contributor;
                        if (contributorIds.Add(contributor.Id))
                        {
                            contributors.Add(contributor);
                        }
                    }
                }
            }

            var author = await GetAuthor();
            if (author != null)
            {
                contributors.RemoveAll(c => c.Id == author.Id);
            }

            if (contributionInfo != null)
            {
                contributionInfo.Author = author;
                contributionInfo.Contributors = contributors;
            }

            return (errors, contributionInfo);

            async Task<Contributor> GetContributor(GitCommit commit)
            {
                if (isGitHubRepo)
                {
                    var (error, githubUser) = await _gitHubUserCache.GetByCommit(
                        commit.AuthorEmail, gitHubOwner, gitHubRepoName, commit.Sha);
                    errors.AddIfNotNull(error);
                    return githubUser?.ToContributor();
                }

                // directly resolve github user by commit email
                return _gitHubUserCache.GetByEmail(commit.AuthorEmail, out var user) ? user?.ToContributor() : default;
            }

            async Task<Contributor> GetAuthor()
            {
                if (!string.IsNullOrEmpty(authorName))
                {
                    // Remove author from contributors if author name is specified
                    var (error, result) = await _gitHubUserCache.GetByLogin(authorName);
                    errors.AddIfNotNull(error);
                    return result?.ToContributor();
                }

                // When author name is not specified, last contributor is author
                return authorFromCommits;
            }

            List<GitCommit> GetContributionCommits()
            {
                var result = commits;
                var bilingual = _fallbackDocset != null && document.Docset.Config.Localization.Bilingual;
                var contributionBranch = bilingual && LocalizationUtility.TryGetContributionBranch(repo.Branch, out var cBranch) ? cBranch : null;
                if (!string.IsNullOrEmpty(contributionBranch))
                {
                    (_, _, result) = _gitCommitProvider.GetCommitHistory(document, contributionBranch);
                }

                return result;
            }
        }

        public DateTime GetUpdatedAt(Document document, List<GitCommit> fileCommits)
        {
            if (fileCommits?.Count > 0)
            {
                return _commitBuildTimeProvider != null
                    && _commitBuildTimeProvider.TryGetCommitBuildTime(fileCommits[0].Sha, out var timeFromHistory)
                    ? timeFromHistory
                    : fileCommits[0].Time.UtcDateTime;
            }

            return _input.TryGetPhysicalPath(document.FilePath, out var physicalPath)
                ? File.GetLastWriteTimeUtc(physicalPath)
                : default;
        }

        public (string contentGitUrl, string originalContentGitUrl, string originalContentGitUrlTemplate, string gitCommit)
            GetGitUrls(Document document)
        {
            Debug.Assert(document != null);

            var isWhitelisted = document.FilePath.Origin == FileOrigin.Default || document.FilePath.Origin == FileOrigin.Fallback;
            var (repo, pathToRepo, commits) = _gitCommitProvider.GetCommitHistory(document);
            if (repo is null)
                return default;

            var (contentBranchUrlTemplate, contentCommitUrlTemplate) = GetContentGitUrlTemplate(repo.Remote, pathToRepo);
            var commit = commits.FirstOrDefault()?.Sha;
            if (string.IsNullOrEmpty(commit))
            {
                commit = repo.Commit;
            }

            var contentGitCommitUrl = contentCommitUrlTemplate?.Replace("{repo}", repo.Remote).Replace("{commit}", commit);
            var originalContentGitUrl = contentBranchUrlTemplate?.Replace("{repo}", repo.Remote).Replace("{branch}", repo.Branch);
            var contentGitUrl = isWhitelisted ? GetContentGitUrl(repo.Remote, repo.Branch, pathToRepo) : originalContentGitUrl;

            return (
                contentGitUrl,
                originalContentGitUrl,
                !isWhitelisted ? originalContentGitUrl : contentBranchUrlTemplate,
                contentGitCommitUrl);
        }

        public void Save()
        {
            if (_commitBuildTimeProvider != null)
            {
                _commitBuildTimeProvider.Save();
            }
        }

        private string GetContentGitUrl(string repo, string branch, string pathToRepo)
        {
            var config = _docset.Config;

            if (!string.IsNullOrEmpty(config.Contribution.RepositoryUrl))
            {
                repo = config.Contribution.RepositoryUrl;
            }

            if (!string.IsNullOrEmpty(config.Contribution.RepositoryBranch))
            {
                branch = config.Contribution.RepositoryBranch;
            }

            if (LocalizationUtility.TryGetContributionBranch(branch, out var contributionBranch))
            {
                branch = contributionBranch;
            }

            if (_fallbackDocset != null)
            {
                (repo, branch) = LocalizationUtility.GetLocalizedRepo(
                    config.Localization.Mapping, false, repo, branch, _docset.Locale, config.Localization.DefaultLocale);
            }

            var (gitUrlTemplate, _) = GetContentGitUrlTemplate(repo, pathToRepo);

            return gitUrlTemplate?.Replace("{repo}", repo).Replace("{branch}", branch);
        }

        private static (string branchUrlTemplate, string commitUrlTemplate) GetContentGitUrlTemplate(string remote, string pathToRepo)
        {
            if (UrlUtility.TryParseGitHubUrl(remote, out _, out _))
            {
                return ($"{{repo}}/blob/{{branch}}/{pathToRepo}", $"{{repo}}/blob/{{commit}}/{pathToRepo}");
            }

            if (UrlUtility.TryParseAzureReposUrl(remote, out _, out _))
            {
                return ($"{{repo}}?path=/{pathToRepo}&version=GB{{branch}}&_a=contents",
                    $"{{repo}}/commit/{{commit}}?path=/{pathToRepo}&_a=contents");
            }

            return default;
        }
    }
}
