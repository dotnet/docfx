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

                var localeToRestore = LocalizationUtility.GetLocale(repository?.Remote, repository?.Branch, options);

                var configPath = docsetPath;
                var (errors, config) = ConfigLoader.TryLoad(configPath, options, localeToRestore, extend: false);

                var restoreFallbackResult = RestoreFallbackRepo(config, repository);
                if (!ConfigLoader.TryGetConfigPath(docsetPath, out _))
                {
                    // build from loc directly with overwrite config
                    // use the fallback config
                    Log.Write("Use config from fallback repository");
                    List<Error> fallbackConfigErrors;
                    configPath = restoreFallbackResult.Path;
                    (fallbackConfigErrors, config) = ConfigLoader.Load(configPath, options, localeToRestore, extend: false);
                    errors.AddRange(fallbackConfigErrors);
                }

                // config error log, and return if config has errors
                errorLog.Configure(config);
                if (errorLog.Write(errors))
                    return;

                // restore extend url firstly
                await ParallelUtility.ForEach(
                    config.Extend.Where(UrlUtility.IsHttp),
                    restoreUrl => RestoreFile.Restore(restoreUrl, config));

                // extend the config after the extend url being restored
                var (extendConfigErrors, extendedConfig) = ConfigLoader.Load(configPath, options, localeToRestore, extend: true);
                errorLog.Write(extendConfigErrors);

                // restore urls except extend url
                var restoreUrls = extendedConfig.GetFileReferences().Where(UrlUtility.IsHttp).ToList();
                await RestoreFile.Restore(restoreUrls, extendedConfig);

                // restore git repos includes dependency repos, theme repo and loc repos
                var restoreDependencyResults = RestoreGit.Restore(extendedConfig, localeToRestore, repository, DependencyLockProvider.Create(docsetPath, extendedConfig.DependencyLock));

                // save dependency lock
                var restoredGitLock = new List<DependencyGitLock>();
                foreach (var restoreResult in restoreDependencyResults.Concat(new[] { restoreFallbackResult }))
                {
                    if (restoreResult != null)
                        restoredGitLock.Add(new DependencyGitLock { Url = restoreResult.Remote, Branch = restoreResult.Branch, Commit = restoreResult.Commit });
                }

                var dependencyLockFilePath = string.IsNullOrEmpty(extendedConfig.DependencyLock)
                    ? AppData.GetDependencyLockFile(docsetPath, localeToRestore)
                    : extendedConfig.DependencyLock;

                DependencyLockProvider.SaveGitLock(docsetPath, dependencyLockFilePath, restoredGitLock);
            }
        }

        private static RestoreGitResult RestoreFallbackRepo(Config config, Repository repository)
        {
            if (LocalizationUtility.TryGetFallbackRepository(repository, out var fallbackRemote, out var fallbackBranch, out _))
            {
                // fallback to master
                if (fallbackBranch != "master" &&
                    !GitUtility.RemoteBranchExists(fallbackRemote, fallbackBranch, config))
                {
                    fallbackBranch = "master";
                }

                var restoredResult = RestoreGit.RestoreGitRepo(config, fallbackRemote, new List<(string branch, RestoreGitFlags flags)> { (fallbackBranch, RestoreGitFlags.None) }, null);
                Debug.Assert(restoredResult.Count == 1);

                return restoredResult[0];
            }

            return default;
        }
    }
}
