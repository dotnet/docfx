// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class ContributionProvider
    {
        private readonly Input _input;
        private readonly Config _config;
        private readonly GitHubAccessor _githubAccessor;
        private readonly BuildOptions _buildOptions;
        private readonly ConcurrentDictionary<string, Lazy<CommitBuildTimeProvider>> _commitBuildTimeProviders =
            new ConcurrentDictionary<string, Lazy<CommitBuildTimeProvider>>(PathUtility.PathComparer);

        private readonly RepositoryProvider _repositoryProvider;
        private readonly SourceMap _sourceMap;

        private readonly ConcurrentDictionary<FilePath, (string?, string?, string?)> _gitUrls =
            new ConcurrentDictionary<FilePath, (string?, string?, string?)>();

        public ContributionProvider(
            Config config, BuildOptions buildOptions, Input input, GitHubAccessor githubAccessor, RepositoryProvider repositoryProvider, SourceMap sourceMap)
        {
            _input = input;
            _config = config;
            _buildOptions = buildOptions;
            _githubAccessor = githubAccessor;
            _repositoryProvider = repositoryProvider;
            _sourceMap = sourceMap;
        }

        public (List<Error> errors, ContributionInfo?) GetContributionInfo(FilePath file, SourceInfo<string> authorName)
        {
            var errors = new List<Error>();

            var fullPath = GetOriginalFullPath(file);
            var (repo, _, commits) = _repositoryProvider.GetCommitHistory(fullPath);
            if (repo is null)
            {
                return (errors, null);
            }

            var updatedDateTime = GetUpdatedAt(file, repo, commits);
            var contributionInfo = new ContributionInfo
            {
                UpdateAt = updatedDateTime.ToString(
                    _buildOptions.Locale == "en-us" ? "M/d/yyyy" : _buildOptions.Culture.DateTimeFormat.ShortDatePattern),
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
                (_, _, contributionCommits) = _repositoryProvider.GetCommitHistory(fullPath, contributionBranch);
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
                    var (error, githubUser) = _githubAccessor.GetUserByEmail(commit.AuthorEmail, repoOwner, repoName, commit.Sha);
                    errors.AddIfNotNull(error);
                    var contributor = githubUser?.ToContributor();
                    if (!string.IsNullOrEmpty(contributor?.Name) && !excludes.Contains(contributor.Name))
                    {
                        contributors.Add(contributor);
                    }
                }
            }

            var author = contributors.Count > 0 ? contributors[^1] : null;
            if (!string.IsNullOrEmpty(authorName))
            {
                // Remove author from contributors if author name is specified
                var (error, githubUser) = _githubAccessor.GetUserByLogin(authorName);
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

        public DateTime GetUpdatedAt(FilePath file, Repository? repository, GitCommit[] fileCommits)
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

            return _input.TryGetPhysicalPath(file, out var physicalPath)
                ? File.GetLastWriteTimeUtc(physicalPath)
                : default;
        }

        public (string? contentGitUrl, string? originalContentGitUrl, string? originalContentGitUrlTemplate)
            GetGitUrl(FilePath file)
        {
            return _gitUrls.GetOrAdd(file, GetGitUrlsCore);

            (string?, string?, string?) GetGitUrlsCore(FilePath file)
            {
                var isWhitelisted = file.Origin == FileOrigin.Main || file.Origin == FileOrigin.Fallback;

                var (repo, pathToRepo) = _repositoryProvider.GetRepository(GetOriginalFullPath(file));
                if (repo is null || pathToRepo is null)
                {
                    return default;
                }

                var gitUrlTemplate = GetGitUrlTemplate(repo.Remote, pathToRepo);
                var originalContentGitUrl = gitUrlTemplate?.Replace("{repo}", repo.Remote).Replace("{branch}", repo.Branch);
                var contentGitUrl = isWhitelisted ? GetContentGitUrl(repo.Remote, repo.Branch, pathToRepo) : originalContentGitUrl;

                return (
                    contentGitUrl,
                    originalContentGitUrl,
                    !isWhitelisted ? originalContentGitUrl : gitUrlTemplate);
            }
        }

        public string? GetGitCommitUrl(FilePath file)
        {
            var (repo, pathToRepo, commits) = _repositoryProvider.GetCommitHistory(GetOriginalFullPath(file));
            if (repo is null || pathToRepo is null)
            {
                return default;
            }

            var commit = commits.Length > 0 ? commits[0].Sha : repo.Commit;

            return UrlUtility.TryParseGitHubUrl(repo.Remote, out _, out _)
                ? $"{repo.Remote}/blob/{commit}/{pathToRepo}"
                : UrlUtility.TryParseAzureReposUrl(repo.Remote, out _, out _)
                ? $"{repo.Remote}/commit/{commit}?path=/{pathToRepo}&_a=contents"
                : null;
        }

        public void Save()
        {
            foreach (var (_, commitBuildTimeProvider) in _commitBuildTimeProviders)
            {
                commitBuildTimeProvider.Value.Save();
            }
        }

        private PathString GetOriginalFullPath(FilePath file)
        {
            var originalPath = _sourceMap.GetOriginalFilePath(file);
            return _input.GetFullPath(originalPath is null ? file : new FilePath(originalPath));
        }

        private string? GetContentGitUrl(string repo, string branch, string pathToRepo)
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

            if (_buildOptions.IsLocalizedBuild)
            {
                repo = LocalizationUtility.GetLocalizedRepository(repo, _buildOptions.Locale);
            }

            var gitUrlTemplate = GetGitUrlTemplate(repo, pathToRepo);

            return gitUrlTemplate?.Replace("{repo}", repo).Replace("{branch}", branch);
        }

        private static string? GetGitUrlTemplate(string remote, string pathToRepo)
        {
            return UrlUtility.TryParseGitHubUrl(remote, out _, out _)
                ? $"{{repo}}/blob/{{branch}}/{pathToRepo}"
                : UrlUtility.TryParseAzureReposUrl(remote, out _, out _)
                ? $"{{repo}}?path=/{pathToRepo}&version=GB{{branch}}&_a=contents"
                : null;
        }
    }
}
