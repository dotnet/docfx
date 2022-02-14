// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build;

internal class PackageResolver
{
    // NOTE: This line assumes each build runs in a new process
    private static readonly ConcurrentDictionary<(string gitRoot, string url, string branch), Lazy<PathString>> s_gitRepositories = new();

    private readonly ErrorBuilder _errors;
    private readonly string _docsetPath;
    private readonly PreloadConfig _config;
    private readonly FetchOptions _fetchOptions;
    private readonly Repository? _repository;
    private readonly FileResolver _fileResolver;

    public PackageResolver(
        ErrorBuilder errors, string docsetPath, PreloadConfig config, FetchOptions fetchOptions, FileResolver fileResolver, Repository? repository)
    {
        _errors = errors;
        _docsetPath = docsetPath;
        _config = config;
        _fetchOptions = fetchOptions;
        _fileResolver = fileResolver;
        _repository = repository;
    }

    public bool TryResolvePackage(PackagePath package, PackageFetchOptions options, [NotNullWhen(true)] out string? path)
    {
        try
        {
            path = ResolvePackage(package, options);
            return true;
        }
        catch (DocfxException)
        {
            path = default;
            return false;
        }
    }

    public Package ResolveAsPackage(PackagePath package, PackageFetchOptions options)
    {
        return package.Type switch
        {
            PackageType.PublicTemplate => new PublicTemplatePackage(package.Url, _fileResolver),
            _ => new LocalPackage(ResolvePackage(package, options)),
        };
    }

    public string ResolvePackage(PackagePath package, PackageFetchOptions options)
    {
        var packagePath = package.Type switch
        {
            PackageType.Git => DownloadGitRepository(package, options),
            _ => Path.Combine(_docsetPath, package.Path),
        };
        if (!Directory.Exists(packagePath) && !options.HasFlag(PackageFetchOptions.IgnoreDirectoryNonExistedError))
        {
            throw Errors.Config.DirectoryNotFound(packagePath).ToException();
        }
        return packagePath;
    }

    public void DownloadPackage(PackagePath package, PackageFetchOptions options)
    {
        switch (package.Type)
        {
            case PackageType.Git:
                DownloadGitRepository(package, options);
                break;
        }
    }

    private PathString DownloadGitRepository(PackagePath path, PackageFetchOptions options)
    {
        return s_gitRepositories.GetOrAdd((AppData.GitRoot, path.Url, path.Branch), _ => new Lazy<PathString>(() =>
            DownloadGitRepositoryCore(
                path.Url,
                path.Branch,
                options))).Value;
    }

    private PathString DownloadGitRepositoryCore(string url, string committish, PackageFetchOptions options)
    {
        var gitPath = GetGitRepositoryPath(url, committish);
        var gitDocfxHead = Path.Combine(gitPath, ".git", "DOCFX_HEAD");

        switch (_fetchOptions)
        {
            case FetchOptions.UseCache when File.Exists(gitDocfxHead):
            case FetchOptions.NoFetch when File.Exists(gitDocfxHead):
                return gitPath;

            case FetchOptions.NoFetch:
                throw Errors.System.NeedRestore($"{url}#{committish}").ToException();
        }

        using (InterProcessMutex.Create(gitPath))
        {
            if (File.Exists(gitDocfxHead))
            {
                File.Delete(gitDocfxHead);
            }

            using (PerfScope.Start($"Downloading '{url}#{committish}'"))
            {
                InitFetchCheckoutGitRepository(gitPath, url, committish, options);
            }
            File.WriteAllText(gitDocfxHead, committish);
            Log.Write($"Repository {url}#{committish} at committish: {GitUtility.GetRepoInfo(gitPath).commit}");
        }

        return gitPath;
    }

    private void InitFetchCheckoutGitRepository(string cwd, string url, string committish, PackageFetchOptions options)
    {
        var fetchOption = "--update-head-ok --prune --force";
        var depthOneOption = $"--depth {(options.HasFlag(PackageFetchOptions.DepthOne) ? "1" : "99999999")}";

        // Remove git lock files if previous build was killed during git operation
        DeleteLockFiles(Path.Combine(cwd, ".git"));

        Directory.CreateDirectory(cwd);
        GitUtility.Init(cwd);
        GitUtility.AddRemote(cwd, "origin", url);

        var succeeded = false;
        var branchUsed = committish;
        foreach (var branch in GitUtility.GetFallbackBranch(committish))
        {
            try
            {
                if (branch != committish)
                {
                    Log.Write($"{committish} branch doesn't exist on repository {url}, fallback to {branch} branch");
                }
                GitUtility.Fetch(_config.Secrets, cwd, url, $"+{branch}:{branch}", $"{fetchOption} {depthOneOption}");
                succeeded = true;
                branchUsed = branch;
                break;
            }
            catch (InvalidOperationException)
            {
            }
        }

        if (!succeeded)
        {
            try
            {
                // Fallback to fetch all branches if the input committish is not supported by fetch
                GitUtility.Fetch(_config.Secrets, cwd, url, "+refs/heads/*:refs/heads/*", $"{fetchOption} --depth 99999999");
            }
            catch (InvalidOperationException ex)
            {
                if (_config.DocsGitTokenType != null)
                {
                    ThrowRestoreDependentRepositoryFailureError(url, committish, ex);
                }

                throw Errors.System.GitCloneFailed(url, committish).ToException(ex);
            }
        }

        if ((branchUsed != committish) && !options.HasFlag(PackageFetchOptions.IgnoreBranchFallbackError))
        {
            _errors.Add(Errors.DependencyRepository.DependencyRepositoryBranchNotMatch(url, committish, branchUsed));
        }

        try
        {
            GitUtility.Checkout(cwd, branchUsed, "--force");
        }
        catch (InvalidOperationException ex)
        {
            throw Errors.Config.CommittishNotFound(url, branchUsed).ToException(ex);
        }
    }

    private void ThrowRestoreDependentRepositoryFailureError(string url, string committish, Exception ex)
    {
        var templateRepoUrlPrefix = "https://github.com/Microsoft/templates.docs.msft";

        if (ex.Message.Contains("has enabled or enforced SAML SSO", StringComparison.OrdinalIgnoreCase))
        {
            if (url.StartsWith(templateRepoUrlPrefix) || _config.DocsGitTokenType.Equals(DocsGitTokenType.SystemServiceAccount))
            {
                throw Errors.DependencyRepository.RestoreDependentRepositoryFailed(url, committish).ToException(ex);
            }
            else
            {
                throw Errors.DependencyRepository.RepositoryOwnerSSOIssue(_repository?.Url, _config.DocsRepositoryOwnerName, url).ToException(ex);
            }
        }
        else if (IsPermissionInsufficient(ex))
        {
            if (_config.DocsGitTokenType.Equals(DocsGitTokenType.SystemServiceAccount))
            {
                if (url.StartsWith(templateRepoUrlPrefix))
                {
                    throw Errors.DependencyRepository.RestoreDependentRepositoryFailed(url, committish).ToException(ex);
                }
                else
                {
                    // Service accounts are not supported for Azure DevOps repos. So this scenario only occurs for GitHub repos.
                    UrlUtility.TryParseGitHubUrl(_repository?.Url, out var repoOrg, out _);
                    throw Errors.DependencyRepository.ServiceAccountPermissionInsufficient(repoOrg, _config.DocsRepositoryOwnerName, url).ToException(ex);
                }
            }
            else
            {
                (var repoOrg, var repoName) = ParseRepoInfo(url);
                throw Errors.DependencyRepository.RepositoryOwnerPermissionInsufficient(
                    _config.DocsRepositoryOwnerName, repoOrg, repoName, url).ToException(ex);
            }
        }
    }

    private static bool IsPermissionInsufficient(Exception e)
    {
        return e.Message.Contains("fatal: Authentication fail", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("remote: Not Found", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("remote: Repository not found.", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("does not exist or you do not have permissions for the operation you are attempting", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("fatal: could not read Username", StringComparison.OrdinalIgnoreCase);
    }

    private static (string? org, string? name) ParseRepoInfo(string url)
    {
        if (UrlUtility.TryParseGitHubUrl(url, out var org, out var name))
        {
            return (org, name);
        }
        else if (UrlUtility.TryParseAzureReposUrl(url, out _, out name, out org))
        {
            return (org, name);
        }
        else
        {
            return default;
        }
    }

    private static PathString GetGitRepositoryPath(string url, string branch)
    {
        return new PathString(Path.Combine(AppData.GitRoot, $"{UrlUtility.UrlToShortName(url)}-{branch}"));
    }

    private static void DeleteLockFiles(string path)
    {
        if (Directory.Exists(path))
        {
            foreach (var file in Directory.GetFiles(path, "*.lock"))
            {
                File.Delete(file);
            }
        }
    }
}
