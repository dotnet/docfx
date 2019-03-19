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

        private readonly Dictionary<string, DateTime> _updateTimeByCommit = new Dictionary<string, DateTime>();

        private readonly GitCommitProvider _gitCommitProvider;

        private ContributionProvider(GitHubUserCache gitHubUserCache, GitCommitProvider gitCommitProvider)
        {
            Debug.Assert(gitCommitProvider != null);

            _gitHubUserCache = gitHubUserCache;
            _gitCommitProvider = gitCommitProvider;
        }

        public static async Task<ContributionProvider> Create(Docset docset, GitHubUserCache cache, GitCommitProvider gitCommitProvider)
        {
            var result = new ContributionProvider(cache, gitCommitProvider);
            await result.LoadCommitsTime(docset);
            return result;
        }

        public async Task<(List<Error> error, Contributor author, List<Contributor> contributors, DateTime updatedAt)> GetAuthorAndContributors(
            Document document,
            string authorName)
        {
            Debug.Assert(document != null);
            var (repo, pathToRepo, commits) = await _gitCommitProvider.GetCommitHistory(document);
            if (repo is null)
            {
                return default;
            }

            var contributionCommits = await GetContributionCommits();

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

            async Task<List<GitCommit>> GetContributionCommits()
            {
                var result = commits;
                var bilingual = document.Docset.IsLocalized() && document.Docset.Config.Localization.Bilingual;
                var contributionBranch = bilingual && LocalizationUtility.TryGetContributionBranch(repo.Branch, out var cBranch) ? cBranch : null;
                if (!string.IsNullOrEmpty(contributionBranch))
                {
                    (_, _, result) = await _gitCommitProvider.GetCommitHistory(document, contributionBranch);
                }

                return result;
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

        public async Task<(string contentGitUrl, string originalContentGitUrl, string originalContentGitUrlTemplate, string gitCommit)>
            GetGitUrls(Document document)
        {
            Debug.Assert(document != null);

            var (repo, pathToRepo, commits) = await _gitCommitProvider.GetCommitHistory(document);
            if (repo is null)
                return default;

            var contentGitUrlTemplate = GetContentCommittishUrlTemplate(repo.Remote, pathToRepo);
            var commit = commits.FirstOrDefault()?.Sha;
            if (string.IsNullOrEmpty(commit))
            {
                commit = repo.Commit;
            }

            var contentGitCommitUrl = contentGitUrlTemplate?.Replace("{repo}", repo.Remote).Replace("{commit-ish}", commit);
            var originalContentGitUrl = contentGitCommitUrl?.Replace(commit, repo.Branch);
            var originalContentGitUrlTemplate = contentGitUrlTemplate?.Replace("{commit-ish}", "{branch}");

            return (GetContentGitUrl(), originalContentGitUrl, originalContentGitUrlTemplate, contentGitCommitUrl);

            string GetContentGitUrl()
            {
                var (editRemote, editBranch) = (repo.Remote, repo.Branch);

                if (LocalizationUtility.TryGetContributionBranch(editBranch, out var repoContributionBranch))
                {
                    editBranch = repoContributionBranch;
                }

                if (!string.IsNullOrEmpty(document.Docset.Config.Contribution.Repository))
                {
                    var (contributionRemote, contributionBranch, hasRefSpec) = HrefUtility.SplitGitHref(document.Docset.Config.Contribution.Repository);
                    contentGitUrlTemplate = GetContentCommittishUrlTemplate(contributionRemote, pathToRepo);

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

                return contentGitUrlTemplate.Replace("{repo}", editRemote).Replace("{commit-ish}", editBranch);
            }
        }

        private async Task LoadCommitsTime(Docset docset)
        {
            if (!string.IsNullOrEmpty(docset.Config.Contribution.GitCommitsTime))
            {
                var (_, content, _) = await RestoreMap.GetRestoredFileContent(docset, docset.Config.Contribution.GitCommitsTime);

                foreach (var commit in JsonUtility.Deserialize<GitCommitsTime>(content).Commits)
                {
                    _updateTimeByCommit.Add(commit.Sha, commit.BuiltAt);
                }
            }
        }

        private enum GitHost
        {
            Unknown,
            GitHub,
            VSTS,
        }

        private GitHost ParseGitHost(string remote)
        {
            if (GitHubUtility.TryParse(remote, out _, out _))
            {
                return GitHost.GitHub;
            }

            if (VstsUtility.TryParse(remote, out _, out _))
            {
                return GitHost.VSTS;
            }

            return GitHost.Unknown;
        }

        private string GetContentCommittishUrlTemplate(string remote, string pathToRepo)
        {
            var gitHost = ParseGitHost(remote);

            switch (gitHost)
            {
                case GitHost.GitHub:
                    return $"{{repo}}/blob/{{commit-ish}}/{pathToRepo}";
                case GitHost.VSTS:
                    return $"{{repo}}/?path={pathToRepo}&branch=GB{{commit-ish}}";
                case GitHost.Unknown:
                    return default;
            }

            return default;
        }
    }
}
