// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class PackageResolver : IDisposable
    {
        internal static Func<string, string> GitRemoteProxy;

        private readonly string _docsetPath;
        private readonly Config _config;
        private readonly bool _noFetch;

        private readonly Dictionary<PathString, InterProcessReaderWriterLock> _gitReaderLocks = new Dictionary<PathString, InterProcessReaderWriterLock>();

        public PackageResolver(string docsetPath, Config config, bool noFetch = false)
        {
            _docsetPath = docsetPath;
            _config = config;
            _noFetch = noFetch;
        }

        public bool TryResolvePackage(PackagePath package, PackageFetchOptions options, out string path)
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

        public string ResolvePackage(PackagePath package, PackageFetchOptions options = PackageFetchOptions.None)
        {
            if (!_noFetch)
            {
                DownloadPackage(package, options);
            }

            switch (package.Type)
            {
                case PackageType.Git:
                    var gitPath = GetGitRepositoryPath(package.Url, package.Branch);
                    EnterGitReaderLock(gitPath);
                    if (!Directory.Exists(gitPath))
                    {
                        throw Errors.NeedRestore($"{package.Url}#{package.Branch}").ToException();
                    }
                    return gitPath;

                default:
                    var dir = Path.Combine(_docsetPath, package.Path);
                    if (!Directory.Exists(dir))
                    {
                        throw Errors.DirectoryNotFound(new SourceInfo<string>(package.Path)).ToException();
                    }
                    return dir;
            }
        }

        public void DownloadPackage(PackagePath path, PackageFetchOptions options = PackageFetchOptions.None)
        {
            try
            {
                using (PerfScope.Start($"Downloading '{path}'"))
                {
                    switch (path.Type)
                    {
                        case PackageType.Git:
                            DownloadGitRepository(path.Url, path.Branch, !options.HasFlag(PackageFetchOptions.FullHistory));
                            break;
                    }
                }
            }
            catch (Exception ex) when (options.HasFlag(PackageFetchOptions.Optional))
            {
                Log.Write($"Ignore optional package download failure '{path}': {ex}");
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

        private void DownloadGitRepository(string url, string committish, bool depthOne)
        {
            var gitPath = GetGitRepositoryPath(url, committish);

            using (InterProcessReaderWriterLock.CreateWriterLock(gitPath))
            {
                if (!Directory.Exists(gitPath))
                {
                    // If the branch is cloned for the first time,
                    // use a staging directory to ensure we don't left with a corrupted git folder
                    var stagingPath = Path.Combine(Path.GetDirectoryName(gitPath), "." + Path.GetFileName(gitPath));
                    DownloadGitRepositoryCore(stagingPath, url, committish, depthOne);
                    Directory.Move(stagingPath, gitPath);
                }
                else
                {
                    // Other wise let git fetch handle the update
                    DownloadGitRepositoryCore(gitPath, url, committish, depthOne);
                }
            }
        }

        private void DownloadGitRepositoryCore(string cwd, string url, string committish, bool depthOne)
        {
            if (!_config.GitShallowFetch)
            {
                depthOne = false;
            }

            Directory.CreateDirectory(cwd);

            GitUtility.Init(cwd);

            var option = "--update-head-ok --prune --force";

            try
            {
                GitUtility.Fetch(_config, cwd, url, $"+{committish}:{committish}", $"{option} --depth {(depthOne ? "1" : "99999999")}");
            }
            catch (InvalidOperationException)
            {
                try
                {
                    // Fallback to fetch all branches if the input committish is not supported by fetch
                    GitUtility.Fetch(_config, cwd, url, "+refs/heads/*:refs/heads/*", $"{option} --depth 99999999");
                }
                catch (InvalidOperationException ex)
                {
                    throw Errors.GitCloneFailed(url, committish).ToException(ex);
                }
            }

            try
            {
                GitUtility.Checkout(cwd, committish, "--force");
            }
            catch (InvalidOperationException ex)
            {
                throw Errors.CommittishNotFound(url, committish).ToException(ex);
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
    }
}
