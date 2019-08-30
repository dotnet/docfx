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

                var (fallbackErrors, fallbackConfig, restoreFallbackResult) = RestoreFallbackRepo(docsetPath, config, repository, localeToRestore, options);
                errors.AddRange(fallbackErrors);

                config = fallbackConfig ?? config;
                errorLog.Configure(config);
                errorLog.Write(errors);

                // restore extend url firstly
                await ParallelUtility.ForEach(
                    config.Extend.Where(UrlUtility.IsHttp),
                    restoreUrl => RestoreFile.Restore(restoreUrl, config));

                // extend the config after the extend url being restored
                var (extendConfigErrors, extendedConfig) = ConfigLoader.TryLoad(restoreFallbackResult?.Path ?? docsetPath, options, localeToRestore, extend: true);
                errorLog.Write(extendConfigErrors);

                // restore urls except extend url
                var restoreUrls = extendedConfig.GetFileReferences().Where(UrlUtility.IsHttp).ToList();
                await RestoreFile.Restore(restoreUrls, extendedConfig);

                // restore git repos includes dependency repos, theme repo and loc repos
                var restoreDependencyResults = RestoreGit.Restore(extendedConfig, localeToRestore, repository, DependencyLockProvider.Load(docsetPath, extendedConfig.DependencyLock));

                // save dependency lock
                foreach (var restoreResult in restoreDependencyResults.Concat(new[] { restoreFallbackResult }))
                {
                    if (restoreResult != null)
                        restoredGitLock.Add($"{restoreResult.Remote}#{restoreResult.Branch}", restoreResult.Commit);
                }

                var dependencyLockFilePath = string.IsNullOrEmpty(extendedConfig.DependencyLock)
                    ? AppData.GetDependencyLockFile(docsetPath, localeToRestore)
                    : extendedConfig.DependencyLock;

                DependencyLockProvider.Save(docsetPath, dependencyLockFilePath, restoredGitLock);
            }
        }

        private static (List<Error> errors, Config config, RestoreGitResult result) RestoreFallbackRepo(string docsetPath, Config config, Repository repository, string locale, CommandLineOptions options)
        {
            var errors = new List<Error>();
            if (LocalizationUtility.TryGetFallbackRepository(repository, out var fallbackRemote, out var fallbackBranch, out _))
            {
                // fallback to master
                if (fallbackBranch != "master" &&
                    !GitUtility.RemoteBranchExists(fallbackRemote, fallbackBranch, config))
                {
                    fallbackBranch = "master";
                }

                var restoredResult = RestoreGit.RestoreGitRepo(config, fallbackRemote, new List<(string branch, GitFlags flags)> { (fallbackBranch, GitFlags.None) }, null);
                Debug.Assert(restoredResult.Count == 1);

                var fallbackRestoreResult = restoredResult[0];
                if (!ConfigLoader.TryGetConfigPath(docsetPath, out _))
                {
                    // build from loc directly with overwrite config
                    // use the fallback config
                    var (fallbackConfigErrors, fallbackConfig) = ConfigLoader.Load(fallbackRestoreResult.Path, options, locale, extend: false);
                    return (fallbackConfigErrors, fallbackConfig, fallbackRestoreResult);
                }

                return (errors, default, fallbackRestoreResult);
            }

            return (errors, default, default);
        }
    }
}
