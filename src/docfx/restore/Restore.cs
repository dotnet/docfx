// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Restore
    {
        public static async Task Run(string docsetPath, CommandLineOptions options, ErrorLog errorLog)
        {
            // Restore has to use Config directly, it cannot depend on Docset,
            // because Docset assumes the repo to physically exist on disk.
            using (Progress.Start("Restore dependencies"))
            {
                var repository = Repository.Create(docsetPath);
                Telemetry.SetRepository(repository?.Remote, repository?.Branch);

                var restoredGitLock = new Dictionary<string, string>();
                var localeToRestore = LocalizationUtility.GetLocale(repository?.Remote, repository?.Branch, options);

                var (errors, config) = ConfigLoader.TryLoad(docsetPath, options, localeToRestore, extend: false);

                if (LocalizationUtility.TryGetFallbackRepository(repository, out var fallbackRemote, out var fallbackBranch, out _))
                {
                    if (!ConfigLoader.TryGetConfigPath(docsetPath, out _))
                    {
                        // build from loc directly with overwrite config
                        // which means fallback repo need to be restored firstly
                        var restoredResult = RestoreGit.RestoreGitRepo(config, fallbackRemote, new List<(string branch, GitFlags flags)> { (fallbackBranch, GitFlags.None) }, null);
                        Debug.Assert(restoredResult.Count == 1);

                        var fallbackRestoreResult = restoredResult[0];
                        (errors, config) = ConfigLoader.Load(fallbackRestoreResult.Path, options, localeToRestore, extend: false);

                        restoredGitLock.Add($"{fallbackRestoreResult.Remote}#{fallbackRestoreResult.Branch}", fallbackRestoreResult.Commit);
                    }
                }

                errorLog.Configure(config);
                errorLog.Write(errors);

                // no need to restore child docsets' loc repository
                var (dependencyLock, dependencyLockFilePath) = await RestoreOneDocset(docsetPath, localeToRestore, config, repository);

                // save dependency lock if it's root entry
                DependencyLockProvider.Save(docsetPath, dependencyLockFilePath, dependencyLock);
            }

            async Task<(Dictionary<string, string> dependencyLock, string dependencyLockFilePath)> RestoreOneDocset(string docset, string locale, Config config, Repository repository)
            {
                // restore extend url firstly
                // no need to extend config
                await ParallelUtility.ForEach(
                    config.Extend.Where(UrlUtility.IsHttp),
                    restoreUrl => RestoreFile.Restore(restoreUrl, config));

                // extend the config before loading
                var (errors, extendedConfig) = ConfigLoader.TryLoad(docset, options, locale, extend: true);
                errorLog.Write(errors);

                // restore and load dependency lock if need
                if (UrlUtility.IsHttp(extendedConfig.DependencyLock))
                    await RestoreFile.Restore(extendedConfig.DependencyLock, extendedConfig);

                var dependencyLock = DependencyLockProvider.Load(docset, extendedConfig.DependencyLock);

                // restore git repos includes dependency repos, theme repo and loc repos
                var gitVersions = RestoreGit.Restore(extendedConfig, locale, repository, dependencyLock);

                // restore urls except extend url
                var restoreUrls = extendedConfig.GetFileReferences().Where(UrlUtility.IsHttp).ToList();
                await RestoreFile.Restore(restoreUrls, extendedConfig);

                return (dependencyLock, string.IsNullOrEmpty(extendedConfig.DependencyLock) ? AppData.GetDependencyLockFile(docset, locale) : extendedConfig.DependencyLock);
            }
        }
    }
}
