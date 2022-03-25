// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal class ContributionProvider
{
    private readonly Input _input;
    private readonly Config _config;
    private readonly GitHubAccessor _githubAccessor;
    private readonly BuildOptions _buildOptions;
    private readonly ConcurrentDictionary<string, Lazy<CommitBuildTimeProvider>> _commitBuildTimeProviders = new(PathUtility.PathComparer);

    private readonly RepositoryProvider _repositoryProvider;

    private readonly ConcurrentDictionary<FilePath, (string?, string?, string?)> _gitUrls = new();

    public ContributionProvider(
        Config config, BuildOptions buildOptions, Input input, GitHubAccessor githubAccessor, RepositoryProvider repositoryProvider)
    {
        _input = input;
        _config = config;
        _buildOptions = buildOptions;
        _githubAccessor = githubAccessor;
        _repositoryProvider = repositoryProvider;
    }

    public (ContributionInfo?, string?[]?) GetContributionInfo(ErrorBuilder errors, FilePath file, SourceInfo<string> authorName)
    {
        var fullPath = _input.TryGetOriginalPhysicalPath(file);
        if (fullPath is null)
        {
            return (null, null);
        }

        var (repo, _, commits) = _repositoryProvider.GetCommitHistory(fullPath.Value);
        if (repo is null)
        {
            return (null, null);
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
            return (contributionInfo, null);
        }

        var contributionCommits = commits;
        var excludes = _config.GlobalMetadata.ContributorsToExclude.Count > 0
            ? _config.GlobalMetadata.ContributorsToExclude
            : _config.ExcludeContributors;

        // Resolve contributors from commits
        var contributors = new List<Contributor>();
        var githubContributors = new List<string>();
        if (UrlUtility.TryParseGitHubUrl(_config.EditRepositoryUrl, out var repoOwner, out var repoName) ||
            UrlUtility.TryParseGitHubUrl(repo.Url, out repoOwner, out repoName))
        {
            foreach (var commit in contributionCommits)
            {
                var (error, githubUser) = _githubAccessor.GetUserByEmail(commit.AuthorEmail, repoOwner, repoName, commit.Sha);
                errors.AddIfNotNull(error);
                var contributor = githubUser?.ToContributor();
                if (!string.IsNullOrEmpty(contributor?.Name))
                {
                    if (!excludes.Contains(contributor.Name))
                    {
                        contributors.Add(contributor);
                    }
                    githubContributors.Add(contributor.Name);
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

        return (contributionInfo, githubContributors.Distinct().ToArray());
    }

    public (string? contentGitUrl, string? originalContentGitUrl, string? originalContentGitUrlTemplate)
        GetGitUrl(FilePath file)
    {
        return _gitUrls.GetOrAdd(file, GetGitUrlsCore);

        (string?, string?, string?) GetGitUrlsCore(FilePath file)
        {
            var isAllowlisted = file.Origin == FileOrigin.Main || file.Origin == FileOrigin.Fallback;

            var fullPath = _input.TryGetOriginalPhysicalPath(file) ?? file?.Path;
            if (fullPath is null)
            {
                return default;
            }

            var (repo, pathToRepo) = _repositoryProvider.GetRepository(fullPath.Value);
            if (repo is null || pathToRepo is null)
            {
                return default;
            }

            var gitUrlTemplate = GetGitUrlTemplate(repo.Url, pathToRepo);
            var originalContentGitUrl = gitUrlTemplate?.Replace("{repo}", repo.Url).Replace("{branch}", repo.Branch);
            var contentGitUrl = isAllowlisted ? GetContentGitUrl(repo.Url, repo.Branch ?? repo.Commit, pathToRepo) : originalContentGitUrl;

            return (
                contentGitUrl,
                originalContentGitUrl,
                !isAllowlisted ? originalContentGitUrl : gitUrlTemplate);
        }
    }

    public string? GetGitCommitUrl(FilePath file)
    {
        var fullPath = _input.TryGetOriginalPhysicalPath(file);
        if (fullPath is null)
        {
            return default;
        }

        var (repo, pathToRepo, commits) = _repositoryProvider.GetCommitHistory(fullPath.Value);
        if (repo is null || pathToRepo is null)
        {
            return default;
        }

        var commit = commits.Length > 0 ? commits[0].Sha : repo.Commit;

        return UrlUtility.TryParseGitHubUrl(repo.Url, out _, out _)
            ? $"{repo.Url}/blob/{commit}/{pathToRepo}"
            : UrlUtility.TryParseAzureReposUrl(repo.Url, out _, out _, out _)
            ? $"{repo.Url}/commit/{commit}?path=/{pathToRepo}&_a=contents"
            : null;
    }

    public void Save()
    {
        foreach (var (_, commitBuildTimeProvider) in _commitBuildTimeProviders)
        {
            commitBuildTimeProvider.Value.Save();
        }
    }

    private string? GetContentGitUrl(string repo, string? committish, string pathToRepo)
    {
        if (!string.IsNullOrEmpty(_config.EditRepositoryUrl))
        {
            repo = _config.EditRepositoryUrl;
        }

        if (!string.IsNullOrEmpty(_config.EditRepositoryBranch))
        {
            committish = _config.EditRepositoryBranch;
        }

        var gitUrlTemplate = GetGitUrlTemplate(repo, pathToRepo);

        return gitUrlTemplate?.Replace("{repo}", repo).Replace("{branch}", committish);
    }

    private static string? GetGitUrlTemplate(string url, string pathToRepo)
    {
        return UrlUtility.TryParseGitHubUrl(url, out _, out _)
            ? $"{{repo}}/blob/{{branch}}/{pathToRepo}"
            : UrlUtility.TryParseAzureReposUrl(url, out _, out _, out _)
            ? $"{{repo}}?path=/{pathToRepo}&version=GB{{branch}}&_a=contents"
            : null;
    }

    private DateTime GetUpdatedAt(FilePath file, Repository? repository, GitCommit[] fileCommits)
    {
        if (fileCommits.Length > 0)
        {
            if (_config.UpdateTimeAsCommitBuildTime && repository != null && repository.Branch != null)
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

        return _input.TryGetOriginalPhysicalPath(file) is PathString
            ? _input.GetLastWriteTimeUtc(file)
            : default;
    }
}
