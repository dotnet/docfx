// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class ContributionProvider
    {
        private readonly Input _input;
        private readonly Config _config;
        private readonly Docset? _fallbackDocset;
        private readonly GitHubAccessor _githubAccessor;
        private readonly LocalizationProvider _localization;

        private readonly ConcurrentDictionary<string, Lazy<CommitBuildTimeProvider>> _commitBuildTimeProviders = new ConcurrentDictionary<string, Lazy<CommitBuildTimeProvider>>(PathUtility.PathComparer);

        private readonly GitCommitProvider _gitCommitProvider;

        public ContributionProvider(
            Config config, LocalizationProvider localization, Input input, Docset? fallbackDocset, GitHubAccessor githubAccessor, GitCommitProvider gitCommitProvider)
        {
            _input = input;
            _config = config;
            _localization = localization;
            _githubAccessor = githubAccessor;
            _gitCommitProvider = gitCommitProvider;
            _fallbackDocset = fallbackDocset;
        }

        public async Task<(List<Error> errors, ContributionInfo?)> GetContributionInfo(Document document, SourceInfo<string> authorName)
        {
            var errors = new List<Error>();
            var (repo, _, commits) = _gitCommitProvider.GetCommitHistory(document);
            if (repo is null)
            {
                return (errors, null);
            }

            var updatedDateTime = GetUpdatedAt(document, repo, commits);
            var contributionInfo = new ContributionInfo
            {
                UpdateAt = updatedDateTime.ToString(
                    _localization.Locale == "en-us" ? "M/d/yyyy" : _localization.Culture.DateTimeFormat.ShortDatePattern),
                UpdatedAtDateTime = updatedDateTime,
            };

            if (!_config.ResolveGithubUsers)
            {
                return (errors, contributionInfo);
            }

            var contributionCommits = commits;
            var contributionBranch = LocalizationUtility.TryGetContributionBranch(repo.Branch, out var cBranch) ? cBranch : null;
            if (!string.IsNullOrEmpty(contributionBranch))
            {
                (_, _, contributionCommits) = _gitCommitProvider.GetCommitHistory(document, contributionBranch);
            }

            var excludes = _config.GlobalMetadata.ContributorsToExclude.Count > 0
                ? _config.GlobalMetadata.ContributorsToExclude
                : _config.ExcludeContributors;

            // Resolve contributors from commits
            var contributors = new List<Contributor>();
            if (UrlUtility.TryParseGitHubUrl(_config.EditRepositoryUrl, out var repoOwner, out var repoName) ||
                UrlUtility.TryParseGitHubUrl(repo.Remote, out repoOwner, out repoName))
            {
                foreach (var commit in contributionCommits)
                {
                    var (error, githubUser) = await _githubAccessor.GetUserByEmail(commit.AuthorEmail, repoOwner, repoName, commit.Sha);
                    errors.AddIfNotNull(error);
                    var contributor = githubUser?.ToContributor();
                    if (contributor != null && !excludes.Contains(contributor.Name))
                    {
                        contributors.Add(contributor);
                    }
                }
            }

            var author = contributors.Count > 0 ? contributors[^1] : null;
            if (!string.IsNullOrEmpty(authorName))
            {
                // Remove author from contributors if author name is specified
                var (error, githubUser) = await _githubAccessor.GetUserByLogin(authorName);
                errors.AddIfNotNull(error);
                author = githubUser?.ToContributor();
            }

            if (author != null)
            {
                contributors.RemoveAll(item => item.Equals(author));
            }

            contributionInfo.Author = author;
            contributionInfo.Contributors = contributors.Distinct().ToArray();

            return (errors, contributionInfo);
        }

        public DateTime GetUpdatedAt(Document document, Repository? repository, GitCommit[] fileCommits)
        {
            if (fileCommits.Length > 0)
            {
                if (_config.UpdateTimeAsCommitBuildTime && repository != null)
                {
                    return _commitBuildTimeProviders
                        .GetOrAdd(repository.Path, new Lazy<CommitBuildTimeProvider>(() => new CommitBuildTimeProvider(_config, repository))).Value
                        .GetCommitBuildTime(fileCommits[0].Sha);
                }
                else
                {
                    return fileCommits[0].Time.UtcDateTime;
                }
            }

            return _input.TryGetPhysicalPath(document.FilePath, out var physicalPath)
                ? File.GetLastWriteTimeUtc(physicalPath)
                : default;
        }

        public (string? contentGitUrl, string? originalContentGitUrl, string? originalContentGitUrlTemplate, string? gitCommit)
            GetGitUrls(Document document)
        {
            var isWhitelisted = document.FilePath.Origin == FileOrigin.Default || document.FilePath.Origin == FileOrigin.Fallback;
            var (repo, pathToRepo, commits) = _gitCommitProvider.GetCommitHistory(document);
            if (repo is null || pathToRepo is null)
                return default;

            var (contentBranchUrlTemplate, contentCommitUrlTemplate) = GetContentGitUrlTemplate(repo.Remote, pathToRepo);
            var commit = commits.FirstOrDefault()?.Sha;
            if (string.IsNullOrEmpty(commit))
            {
                commit = repo.Commit;
            }

            var contentGitCommitUrl = contentCommitUrlTemplate?.Replace("{repo}", repo.Remote).Replace("{commit}", commit);
            var originalContentGitUrl = contentBranchUrlTemplate?.Replace("{repo}", repo.Remote).Replace("{branch}", repo.Branch);
            var contentGitUrl = isWhitelisted ? GetContentGitUrl(repo.Remote, repo.Branch, pathToRepo, _localization.Locale) : originalContentGitUrl;

            return (
                contentGitUrl,
                originalContentGitUrl,
                !isWhitelisted ? originalContentGitUrl : contentBranchUrlTemplate,
                contentGitCommitUrl);
        }

        public void Save()
        {
            foreach (var (_, commitBuildTimeProvider) in _commitBuildTimeProviders)
            {
                commitBuildTimeProvider.Value.Save();
            }
        }

        private string? GetContentGitUrl(string repo, string branch, string pathToRepo, string locale)
        {
            if (!string.IsNullOrEmpty(_config.EditRepositoryUrl))
            {
                repo = _config.EditRepositoryUrl;
            }

            if (!string.IsNullOrEmpty(_config.EditRepositoryBranch))
            {
                branch = _config.EditRepositoryBranch;
            }

            if (LocalizationUtility.TryGetContributionBranch(branch, out var contributionBranch))
            {
                branch = contributionBranch;
            }

            if (_fallbackDocset != null)
            {
                (repo, branch) = LocalizationUtility.GetLocalizedRepo(false, repo, branch, locale, _config.DefaultLocale);
            }

            var (gitUrlTemplate, _) = GetContentGitUrlTemplate(repo, pathToRepo);

            return gitUrlTemplate?.Replace("{repo}", repo).Replace("{branch}", branch);
        }

        private static (string? branchUrlTemplate, string? commitUrlTemplate) GetContentGitUrlTemplate(string remote, string pathToRepo)
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
