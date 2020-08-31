// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class PackageResolver : IDisposable
    {
        // NOTE: This line assumes each build runs in a new process
        private static readonly ConcurrentDictionary<(string gitRoot, string url, string branch), Lazy<PathString>> s_gitRepositories
                          = new ConcurrentDictionary<(string gitRoot, string url, string branch), Lazy<PathString>>();

        private readonly string _docsetPath;
        private readonly PreloadConfig _config;
        private readonly FetchOptions _fetchOptions;
        private readonly Repository? _repository;

        private readonly Dictionary<PathString, InterProcessReaderWriterLock> _gitReaderLocks = new Dictionary<PathString, InterProcessReaderWriterLock>();

        public PackageResolver(string docsetPath, PreloadConfig config, FetchOptions fetchOptions, Repository? repository)
        {
            _docsetPath = docsetPath;
            _config = config;
            _fetchOptions = fetchOptions;
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

        public string ResolvePackage(PackagePath package, PackageFetchOptions options)
        {
            switch (package.Type)
            {
                case PackageType.Git:
                    var gitPath = DownloadGitRepository(package, options);
                    EnterGitReaderLock(gitPath);
                    return gitPath;

                default:
                    var dir = Path.Combine(_docsetPath, package.Path);
                    if (!Directory.Exists(dir))
                    {
                        throw Errors.Config.DirectoryNotFound(new SourceInfo<string>(package.Path)).ToException();
                    }
                    return dir;
            }
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

        public void Dispose()
        {
            lock (_gitReaderLocks)
            {
                foreach (var item in _gitReaderLocks.Values)
                {
                    item.Dispose();
                }
            }
        }

        private PathString DownloadGitRepository(PackagePath path, PackageFetchOptions options)
        {
            return s_gitRepositories.GetOrAdd((AppData.GitRoot, path.Url, path.Branch), _ => new Lazy<PathString>(() =>
                DownloadGitRepositoryCore(
                    path.Url,
                    path.Branch,
                    options.HasFlag(PackageFetchOptions.DepthOne),
                    path is DependencyConfig dependency && dependency.IncludeInBuild))).Value;
        }

        private PathString DownloadGitRepositoryCore(string url, string committish, bool depthOne, bool fetchContributionBranch)
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

            using (InterProcessReaderWriterLock.CreateWriterLock(gitPath))
            {
                DeleteIfExist(gitDocfxHead);
                using (PerfScope.Start($"Downloading '{url}#{committish}'"))
                {
                    InitFetchCheckoutGitRepository(gitPath, url, committish, depthOne);
                }
                File.WriteAllText(gitDocfxHead, committish);
                Log.Write($"Repository {url}#{committish} at committish: {GitUtility.GetRepoInfo(gitPath).commit}");
            }

            // Ensure contribution branch for CRR included in build
            if (fetchContributionBranch)
            {
                var crrRepository = Repository.Create(gitPath, committish, url);
                LocalizationUtility.EnsureLocalizationContributionBranch(_config, crrRepository);
            }

            return gitPath;
        }

        private void InitFetchCheckoutGitRepository(string cwd, string url, string committish, bool depthOne)
        {
            var fetchOption = "--update-head-ok --prune --force";
            var depthOneOption = $"--depth {(depthOne ? "1" : "99999999")}";

            Directory.CreateDirectory(cwd);
            GitUtility.Init(cwd);
            GitUtility.AddRemote(cwd, "origin", url);

            // Remove git lock files if previous build was killed during git operation
            DeleteIfExist(Path.Combine(cwd, ".git/index.lock"));
            DeleteIfExist(Path.Combine(cwd, ".git/shallow.lock"));

            var succeeded = false;
            foreach (var branch in GitUtility.GetFallbackBranch(committish))
            {
                try
                {
                    if (branch != committish)
                    {
                        Log.Write($"{committish} branch doesn't exist on repository {url}, fallback to {branch} branch");
                    }
                    GitUtility.Fetch(_config, cwd, url, $"+{branch}:{branch}", $"{fetchOption} {depthOneOption}");
                    succeeded = true;
                    committish = branch;
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
                    GitUtility.Fetch(_config, cwd, url, "+refs/heads/*:refs/heads/*", $"{fetchOption} --depth 99999999");
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

            try
            {
                GitUtility.Checkout(cwd, committish, "--force");
            }
            catch (InvalidOperationException ex)
            {
                throw Errors.Config.CommittishNotFound(url, committish).ToException(ex);
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
                    throw Errors.DependencyRepository.RepositoryOwnerSSOIssue(_repository?.Remote, _config.DocsRepositoryOwnerName, url).ToException(ex);
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
                        UrlUtility.TryParseGitHubUrl(_repository?.Remote, out var repoOrg, out _);
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

        private static bool IsPermissionInsufficient(Exception ex)
        {
            return ex.Message.Contains("fatal: Authentication fail", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains("remote: Not Found", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains("remote: Repository not found.", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains(
                                "does not exist or you do not have permissions for the operation you are attempting", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains("fatal: could not read Username", StringComparison.OrdinalIgnoreCase);
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
            return new PathString(Path.Combine(AppData.GitRoot, $"{PathUtility.UrlToShortName(url)}-{branch}"));
        }

        private void EnterGitReaderLock(PathString gitPath)
        {
            lock (_gitReaderLocks)
            {
                if (!_gitReaderLocks.ContainsKey(gitPath))
                {
                    _gitReaderLocks.Add(gitPath, InterProcessReaderWriterLock.CreateReaderLock(gitPath));
                }
            }
        }

        private static void DeleteIfExist(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
