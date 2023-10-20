// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Docfx.Plugins;
using GitReader.Primitive;
using GitReader;

#nullable enable

namespace Docfx.Common.Git;

public static class GitUtility
{
    record Repo(string path, string url, string branch);

    private static Regex GitHubRepoUrlRegex =
        new(@"^((https|http):\/\/(.+@)?github\.com\/|git@github\.com:)(?<account>\S+)\/(?<repository>[A-Za-z0-9_.-]+)(\.git)?\/?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

    private static readonly Regex VsoGitRepoUrlRegex =
        new(@"^(((https|http):\/\/(?<account>\S+))|((ssh:\/\/)(?<account>\S+)@(?:\S+)))\.visualstudio\.com(?<port>:\d+)?(?:\/DefaultCollection)?(\/(?<project>[^\/]+)(\/.*)*)*\/(?:_git|_ssh)\/(?<repository>([^._,]|[^._,][^@~;{}'+=,<>|\/\\?:&$*""#[\]]*[^.,]))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string GitHubNormalizedRepoUrlTemplate = "https://github.com/{0}/{1}";
    private static readonly string VsoNormalizedRepoUrlTemplate = "https://{0}.visualstudio.com/DefaultCollection/{1}/_git/{2}";

    private static readonly ConcurrentDictionary<string, Repo?> s_cache = new();

    private static readonly string? s_branch =
        Env("DOCFX_SOURCE_BRANCH_NAME") ?? 
        Env("GITHUB_REF_NAME") ??  // GitHub Actions
        Env("APPVEYOR_REPO_BRANCH") ?? // AppVeyor
        Env("Git_Branch") ?? // Team City
        Env("CI_BUILD_REF_NAME") ?? // GitLab CI
        Env("GIT_LOCAL_BRANCH") ??// Jenkins
        Env("GIT_BRANCH") ?? // Jenkins
        Env("BUILD_SOURCEBRANCHNAME"); // VSO Agent

    private static string? Env(string name) => Environment.GetEnvironmentVariable(name) is { } value && !string.IsNullOrEmpty(value) ? value : null;

    public static GitDetail? TryGetFileDetail(string filePath)
    {
        if (EnvironmentContext.GitFeaturesDisabled)
            return null;

        var repo = GetRepoInfo(Path.GetDirectoryName(filePath));
        if (repo is null)
            return null;

        return new()
        {
            Repo = repo.url,
            Branch = repo.branch,
            Path = Path.GetRelativePath(repo.path, filePath),
        };
    }

    public static string? RawContentUrlToContentUrl(string rawUrl)
    {
        if (EnvironmentContext.GitFeaturesDisabled)
            return null;

        // GitHub
        return Regex.Replace(
            rawUrl,
            @"^https://raw\.githubusercontent\.com/([^/]+)/([^/]+)/([^/]+)/(.+)$",
            string.IsNullOrEmpty(s_branch) ? "https://github.com/$1/$2/blob/$3/$4" : $"https://github.com/$1/$2/blob/{s_branch}/$4");
    }

    private static Repo? GetRepoInfo(string? directory)
    {
        if (string.IsNullOrEmpty(directory))
            return null;

        return s_cache.GetOrAdd(directory, _ =>
        {
            if (IsGitRoot(directory))
            {
                return GetRepoInfoCore(directory).Result;
            }

            return GetRepoInfo(Path.GetDirectoryName(directory));
        });

        static async Task<Repo?> GetRepoInfoCore(string directory)
        {
            using var repo = await Repository.Factory.OpenPrimitiveAsync(directory);

            var url = repo.RemoteUrls.FirstOrDefault(r => r.Key == "origin").Value
                ?? repo.RemoteUrls.FirstOrDefault().Value;

            if (string.IsNullOrEmpty(url))
                return null;

            var branch = s_branch ?? await GetBranchName();
            if (string.IsNullOrEmpty(branch))
                return null;

            return new(directory, url, branch);

            async Task<string?> GetBranchName()
            {
                if (await repo.GetCurrentHeadReferenceAsync() is { } head)
                {
                    return head.Name == "HEAD" ? head.Target.ToString() : head.Name;
                }
                return null;
            }
        }

        static bool IsGitRoot(string directory)
        {
            var gitPath = Path.Combine(directory, ".git");

            // GitReader does not support .git as file in submodules
            // https://github.com/kekyo/GitReader/blob/4126738704ee8b6e330e99c6bc81e9de8bc925c9/GitReader.Core/Primitive/PrimitiveRepositoryFacade.cs#L27-L30
            return Directory.Exists(gitPath);
        }
    }

    [Obsolete("Docfx parses repoUrl in template preprocessor. This method is never used.")]
    public static GitRepoInfo Parse(string repoUrl)
    {
#if NET7_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(repoUrl);
#else
        if (string.IsNullOrEmpty(repoUrl))
        {
            throw new ArgumentNullException(nameof(repoUrl));
        }
#endif

        var githubMatch = GitHubRepoUrlRegex.Match(repoUrl);
        if (githubMatch.Success)
        {
            var gitRepositoryAccount = githubMatch.Groups["account"].Value;
            var gitRepositoryName = githubMatch.Groups["repository"].Value;
            return new GitRepoInfo
            {
                RepoType = RepoType.GitHub,
                RepoAccount = gitRepositoryAccount,
                RepoName = gitRepositoryName,
                RepoProject = null,
                NormalizedRepoUrl = new Uri(string.Format(GitHubNormalizedRepoUrlTemplate, gitRepositoryAccount, gitRepositoryName))
            };
        }

        var vsoMatch = VsoGitRepoUrlRegex.Match(repoUrl);
        if (vsoMatch.Success)
        {
            var gitRepositoryAccount = vsoMatch.Groups["account"].Value;
            var gitRepositoryName = vsoMatch.Groups["repository"].Value;

            // VSO has this logic: if the project name and repository name are same, then VSO will return the url without project name.
            // Sample: if you visit https://cpubwin.visualstudio.com/drivers/_git/drivers, it will return https://cpubwin.visualstudio.com/_git/drivers
            // We need to normalize it to keep same behavior with other projects. Always return https://<account>.visualstudio.com/<collection>/<project>/_git/<repository>
            var gitRepositoryProject = string.IsNullOrEmpty(vsoMatch.Groups["project"].Value) ? gitRepositoryName : vsoMatch.Groups["project"].Value;
            return new GitRepoInfo
            {
                RepoType = RepoType.Vso,
                RepoAccount = gitRepositoryAccount,
                RepoName = Uri.UnescapeDataString(gitRepositoryName),
                RepoProject = gitRepositoryProject,
                NormalizedRepoUrl = new Uri(string.Format(VsoNormalizedRepoUrlTemplate, gitRepositoryAccount, gitRepositoryProject, gitRepositoryName))
            };
        }

        throw new NotSupportedException($"'{repoUrl}' is not a valid Vso/GitHub repository url");
    }
}
