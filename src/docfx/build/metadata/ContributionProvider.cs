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
        private readonly GitHubUserCache _gitHubUserCache;

        // TODO: support CRR and multiple repositories
        // TODO: support live SXS branch
        private readonly CommitBuildTimeProvider _commitBuildTimeProvider;

        private readonly GitCommitProvider _gitCommitProvider;

        public ContributionProvider(Docset docset, GitHubUserCache gitHubUserCache, GitCommitProvider gitCommitProvider)
        {
            Debug.Assert(gitCommitProvider != null);

            _gitHubUserCache = gitHubUserCache;
            _gitCommitProvider = gitCommitProvider;
            _commitBuildTimeProvider = docset.Repository != null && docset.Config.UpdateTimeAsCommitBuildTime
                ? new CommitBuildTimeProvider(docset.Repository) : null;
        }

        public async Task<(List<Error> errors, ContributionInfo contributionInfo)> GetContributionInfo(Document document, SourceInfo<string> authorName)
        {
            Debug.Assert(document != null);
            var (repo, pathToRepo, commits) = _gitCommitProvider.GetCommitHistory(document);
            if (repo is null)
            {
                return default;
            }

            var contributionCommits = GetContributionCommits();

            var excludes = document.Docset.Config.Contribution.ExcludedContributors;

            var contributors = new List<Contributor>();
            var errors = new List<Error>();
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var userIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var updatedDateTime = GetUpdatedAt(document, commits);
            var contributionInfo = updatedDateTime != default
                ? new ContributionInfo
                {
                    Contributors = contributors,
                    UpdateAt = updatedDateTime.ToString(document.Docset.Locale == "en-us" ? "M/d/yyyy" : document.Docset.Culture.DateTimeFormat.ShortDatePattern),
                    UpdatedAtDateTime = updatedDateTime,
                }
                : null;

            var isGitHubRepo = UrlUtility.TryParseGitHubUrl(repo?.Remote, out var gitHubOwner, out var gitHubRepoName) ||
                UrlUtility.TryParseGitHubUrl(document.Docset.Config.Contribution.Repository, out gitHubOwner, out gitHubRepoName);

            if (!isGitHubRepo && !document.Docset.Config.GitHub.ResolveUsers)
            {
                return (errors, contributionInfo);
            }

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
                    var (error, githubUser) = await _gitHubUserCache.GetByCommit(commit.AuthorEmail, gitHubOwner, gitHubRepoName, commit.Sha);
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
                    errors.AddIfNotNull(error?.WithSourceInfo(authorName));
                    return result?.ToContributor();
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

            List<GitCommit> GetContributionCommits()
            {
                var result = commits;
                var bilingual = document.Docset.IsLocalized() && document.Docset.Config.Localization.Bilingual;
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
                return _commitBuildTimeProvider != null && _commitBuildTimeProvider.TryGetCommitBuildTime(fileCommits[0].Sha, out var timeFromHistory)
                    ? timeFromHistory
                    : fileCommits[0].Time.UtcDateTime;
            }
            return File.GetLastWriteTimeUtc(Path.Combine(document.Docset.DocsetPath, document.FilePath));
        }

        public (string contentGitUrl, string originalContentGitUrl, string originalContentGitUrlTemplate, string gitCommit)
            GetGitUrls(Document document)
        {
            Debug.Assert(document != null);

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
            var originalContentGitUrlTemplate = contentBranchUrlTemplate;
            var originalContentGitUrl = originalContentGitUrlTemplate?.Replace("{repo}", repo.Remote).Replace("{branch}", repo.Branch);

            return (GetContentGitUrl(contentBranchUrlTemplate), originalContentGitUrl, originalContentGitUrlTemplate, contentGitCommitUrl);

            string GetContentGitUrl(string branchUrlTemplate)
            {
                var (editRemote, editBranch) = (repo.Remote, repo.Branch);

                if (LocalizationUtility.TryGetContributionBranch(editBranch, out var repoContributionBranch))
                {
                    editBranch = repoContributionBranch;
                }

                if (!string.IsNullOrEmpty(document.Docset.Config.Contribution.Repository))
                {
                    var (contributionRemote, contributionBranch, hasRefSpec) = UrlUtility.SplitGitUrl(document.Docset.Config.Contribution.Repository);
                    (branchUrlTemplate, _) = GetContentGitUrlTemplate(contributionRemote, pathToRepo);

                    (editRemote, editBranch) = (contributionRemote, hasRefSpec ? contributionBranch : editBranch);
                    if (document.Docset.IsLocalized())
                    {
                        (editRemote, editBranch) = LocalizationUtility.GetLocalizedRepo(
                                                    document.Docset.Config.Localization.Mapping,
                                                    false,
                                                    editRemote,
                                                    editBranch,
                                                    document.Docset.Locale,
                                                    document.Docset.Config.Localization.DefaultLocale);
                    }
                }

                return branchUrlTemplate?.Replace("{repo}", editRemote).Replace("{branch}", editBranch);
            }
        }

        public void UpdateCommitBuildTime()
        {
            if (_commitBuildTimeProvider != null)
            {
                _commitBuildTimeProvider.UpdateAndSaveCache();
            }
        }

        private static (string branchUrlTemplate, string commitUrlTemplate) GetContentGitUrlTemplate(string remote, string pathToRepo)
        {
            if (UrlUtility.TryParseGitHubUrl(remote, out _, out _))
            {
                return ($"{{repo}}/blob/{{branch}}/{pathToRepo}", $"{{repo}}/blob/{{commit}}/{pathToRepo}");
            }

            if (UrlUtility.TryParseAzureReposUrl(remote, out _, out _))
            {
                return ($"{{repo}}?path=/{pathToRepo}&version=GB{{branch}}&_a=contents", $"{{repo}}/commit/{{commit}}?path=/{pathToRepo}&_a=contents");
            }

            return default;
        }
    }
}
