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
        private static readonly ConcurrentHashSet<(string, bool)> s_downloadedGitRepositories = new ConcurrentHashSet<(string, bool)>();

        private readonly string _docsetPath;
        private readonly PreloadConfig _config;
        private readonly FetchOptions _fetchOptions;
        private readonly Repository? _repository;

        private readonly ConcurrentDictionary<PackagePath, Lazy<string>> _packagePath = new ConcurrentDictionary<PackagePath, Lazy<string>>();
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
            return _packagePath.GetOrAdd(package, key => new Lazy<string>(() => ResolvePackageCore(key, options))).Value;
        }

        public void DownloadPackage(PackagePath path, PackageFetchOptions options)
        {
            if (path.Type == PackageType.Git)
            {
                DownloadGitRepository(
                    path.Url,
                    path.Branch,
                    options.HasFlag(PackageFetchOptions.DepthOne),
                    path is DependencyConfig dependency && dependency.IncludeInBuild);
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

        private string ResolvePackageCore(PackagePath package, PackageFetchOptions options)
        {
            if (_fetchOptions != FetchOptions.NoFetch)
            {
                DownloadPackage(package, options);
            }

            switch (package.Type)
            {
                case PackageType.Git:
                    var gitPath = GetGitRepositoryPath(package.Url, package.Branch);
                    var gitDocfxHead = Path.Combine(gitPath, ".git", "DOCFX_HEAD");
                    EnterGitReaderLock(gitPath);

                    if (!File.Exists(gitDocfxHead))
                    {
                        throw Errors.System.NeedRestore($"{package.Url}#{package.Branch}").ToException();
                    }
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

        private void DownloadGitRepository(string url, string committish, bool depthOne, bool fetchContributionBranch)
        {
            var gitPath = GetGitRepositoryPath(url, committish);
            var gitDocfxHead = Path.Combine(gitPath, ".git", "DOCFX_HEAD");

            if (_fetchOptions == FetchOptions.UseCache && File.Exists(gitDocfxHead))
            {
                return;
            }

            using (InterProcessReaderWriterLock.CreateWriterLock(gitPath))
            {
                if (!s_downloadedGitRepositories.Contains((gitPath, depthOne)))
                {
                    DeleteIfExist(gitDocfxHead);
                    using (PerfScope.Start($"Downloading '{url}#{committish}'"))
                    {
                        DownloadGitRepositoryCore(gitPath, url, committish, depthOne);
                    }
                    File.WriteAllText(gitDocfxHead, committish);
                    Log.Write($"Repository {url}#{committish} at committish: {GitUtility.GetRepoInfo(gitPath).commit}");

                    s_downloadedGitRepositories.TryAdd((gitPath, depthOne));
                }
            }

            // ensure contribution branch for CRR included in build
            if (fetchContributionBranch)
            {
                var crrRepository = Repository.Create(gitPath, committish, url);
                LocalizationUtility.EnsureLocalizationContributionBranch(_config, crrRepository);
            }
        }

        private void DownloadGitRepositoryCore(string cwd, string url, string committish, bool depthOne)
        {
            var fetchOption = "--update-head-ok --prune --force";
            var depthOneOption = $"--depth {(depthOne ? "1" : "99999999")}";

            Directory.CreateDirectory(cwd);
            GitUtility.Init(cwd);
            GitUtility.AddRemote(cwd, "origin", url);

            // Remove git lock files if previous build was killed during git operation
            DeleteIfExist(Path.Combine(cwd, ".git/index.lock"));
            DeleteIfExist(Path.Combine(cwd, ".git/shallow.lock"));

            try
            {
                GitUtility.Fetch(_config, cwd, url, $"+{committish}:{committish}", $"{fetchOption} {depthOneOption}");
            }
            catch (InvalidOperationException)
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
                    throw Errors.System.RestoreDependentRepositoryFailed(url, committish).ToException(ex);
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
                        throw Errors.System.RestoreDependentRepositoryFailed(url, committish).ToException(ex);
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
                    throw Errors.DependencyRepository.RepositoryOwnerPermissionInsufficient(_config.DocsRepositoryOwnerName, repoOrg, repoName, url).ToException(ex);
                }
            }
        }

        private static bool IsPermissionInsufficient(Exception ex)
        {
            return ex.Message.Contains("fatal: Authentication fail", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains("remote: Not Found", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains("remote: Repository not found.", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains("does not exist or you do not have permissions for the operation you are attempting", StringComparison.OrdinalIgnoreCase)
                            || ex.Message.Contains("fatal: could not read Username", StringComparison.OrdinalIgnoreCase);
        }

        private (string? org, string? name) ParseRepoInfo(string url)
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

        private PathString GetGitRepositoryPath(string url, string branch)
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
