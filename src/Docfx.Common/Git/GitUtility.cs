// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Docfx.Plugins;

#nullable enable

namespace Docfx.Common.Git;

public record GitSource(string Repo, string Branch, string Path, int Line);

public static partial class GitUtility
{
    record Repo(string path, string url, string branch);

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

        var repoUrl = ResolveDocfxSourceRepoUrl(repo.url);

        return new()
        {
            Repo = repoUrl,
            Branch = repo.branch,
            Path = Path.GetRelativePath(repo.path, filePath).Replace('\\', '/'),
        };
    }

    public static string RawContentUrlToContentUrl(string rawUrl)
    {
        // GitHub
        var url = GitHubUserContentRegex().Replace(rawUrl, string.IsNullOrEmpty(s_branch) ? "https://github.com/$1/$2/blob/$3/$4" : $"https://github.com/$1/$2/blob/{s_branch}/$4");

        return ResolveDocfxSourceRepoUrl(url);
    }

    [GeneratedRegex(@"^https://raw\.githubusercontent\.com/([^/]+)/([^/]+)/([^/]+)/(.+)$")]
    private static partial Regex GitHubUserContentRegex();

    public static string? GetSourceUrl(GitSource source)
    {
        var repo = source.Repo.StartsWith("git") ? GitUrlToHttps(source.Repo) : source.Repo;
        repo = repo.TrimEnd('/').TrimEnd(".git");

        if (!Uri.TryCreate(repo, UriKind.Absolute, out var url))
            return null;

        var path = source.Path.Replace('\\', '/');

        var sourceUrl = url.Host switch
        {
            "github.com" => $"https://github.com{url.AbsolutePath}/blob/{source.Branch}/{path}{(source.Line > 0 ? $"#L{source.Line}" : null)}",
            "bitbucket.org" => $"https://bitbucket.org{url.AbsolutePath}/src/{source.Branch}/{path}{(source.Line > 0 ? $"#lines-{source.Line}" : null)}",
            _ when url.Host.EndsWith(".visualstudio.com") || url.Host == "dev.azure.com" =>
                $"https://{url.Host}{url.AbsolutePath}?path={path}&version={(IsCommit(source.Branch) ? "GC" : "GB")}{source.Branch}{(source.Line > 0 ? $"&line={source.Line}" : null)}",
            _ => null,
        };

        if (sourceUrl == null)
            return null;

        return ResolveDocfxSourceRepoUrl(sourceUrl);

        static bool IsCommit(string branch)
        {
            return branch.Length == 40 && branch.All(char.IsLetterOrDigit);
        }

        static string GitUrlToHttps(string url)
        {
            var pos = url.IndexOf('@');
            if (pos == -1) return url;
            return $"https://{url.Substring(pos + 1).Replace(":[0-9]+", "").Replace(':', '/')}";
        }
    }

    private const string GitDir = "gitdir: ";

    private static Repo? GetRepoInfo(string? directory)
    {
        if (string.IsNullOrEmpty(directory))
            return null;

        return s_cache.GetOrAdd(directory, _ =>
        {
            var gitRoot = GetRepoDatabase(directory);
            if (gitRoot != null)
            {
                return GetRepoInfoCore(directory, gitRoot);
            }

            return GetRepoInfo(Path.GetDirectoryName(directory));
        });

        static Repo? GetRepoInfoCore(string directory, string gitRoot)
        {
            var remoteUrls = ParseRemoteUrls(gitRoot).ToArray();
            var url = remoteUrls.FirstOrDefault(r => r.key == "origin").value ?? remoteUrls.FirstOrDefault().value;
            if (string.IsNullOrEmpty(url))
                return null;

            var branch = s_branch ?? GetBranchName();
            if (string.IsNullOrEmpty(branch))
                return null;

            return new(directory, url, branch);

            string? GetBranchName()
            {
                var headPath = Path.Combine(directory, ".git", "HEAD");
                var head = File.Exists(headPath) ? File.ReadAllText(headPath).Trim() : null;
                if (head == null)
                    return null;

                if (head.StartsWith("ref: "))
                    return head.Substring("ref: ".Length).Replace("refs/heads/", "").Replace("refs/remotes/", "").Replace("refs/tags/", "");

                return head;
            }
        }

        static string? GetRepoDatabase(string directory)
        {
            var gitPath = Path.Combine(directory, ".git");
            if (Directory.Exists(gitPath))
                return gitPath;

            if (File.Exists(gitPath))
            {
                var firstLine = File.ReadLines(gitPath).FirstOrDefault();
                if (firstLine != null && firstLine.StartsWith(GitDir))
                    return Path.Combine(directory, firstLine.Substring(GitDir.Length));
            }

            return null;
        }

        static IEnumerable<(string key, string value)> ParseRemoteUrls(string gitRoot)
        {
            var configPath = Path.Combine(gitRoot, "config");
            if (!File.Exists(configPath))
                yield break;

            var key = "";

            foreach (var text in File.ReadAllLines(configPath))
            {
                var line = text.Trim();
                if (line.StartsWith("["))
                {
                    var remote = RemoteRegex().Replace(line, "$1");
                    key = remote != line ? remote : "";
                }
                else if (line.StartsWith("url = ") && !string.IsNullOrEmpty(key))
                {
                    var value = line.Substring("url = ".Length).Trim();
                    yield return (key, value);
                }
            }
        }
    }
    [GeneratedRegex(@"\[remote\s+\""(.+)?\""\]")]
    private static partial Regex RemoteRegex();

    /// <summary>
    /// Rewrite path if `DOCFX_SOURCE_REPOSITORY_URL` environment variable is specified.
    /// </summary>
    private static string ResolveDocfxSourceRepoUrl(string originalUrl)
    {
        var docfxSourceRepoUrl = Environment.GetEnvironmentVariable("DOCFX_SOURCE_REPOSITORY_URL");
        if (docfxSourceRepoUrl == null)
            return originalUrl;

        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var parsedOriginalUrl)
         || !Uri.TryCreate(docfxSourceRepoUrl, UriKind.Absolute, out var parsedOverrideUrl)
         || parsedOriginalUrl.Host != parsedOverrideUrl.Host)
        {
            return originalUrl;
        }

        // Parse value that defined with `{orgName}/{repoName}` format.
        var parts = parsedOverrideUrl.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return originalUrl;

        string orgName = parts[0];
        string repoName = parts[1];

        switch (parsedOriginalUrl.Host)
        {
            case "github.com":
            case "bitbucket.org":
            case "dev.azure.com":
                {
                    // Replace `/{orgName}/{repoName}` and remove `.git` suffix.
                    var builder = new UriBuilder(parsedOriginalUrl);
                    builder.Path = OrgRepoRegex().Replace(builder.Path.TrimEnd(".git"), $"/{orgName}/{repoName}");
                    return builder.Uri.ToString();
                }

            // Currently other URL formats are not supported (e.g. visualstudio.com, GitHub Enterprise Server)
            default:
                return originalUrl;
        }
    }

    [GeneratedRegex("^/[^/]+/[^/]+")]
    private static partial Regex OrgRepoRegex();
}
