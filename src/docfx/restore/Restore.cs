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
                // load and trace entry repository
                var repositoryProvider = new RepositoryProvider(docsetPath, options);
                var repository = repositoryProvider.GetRepository(FileOrigin.Default);
                Telemetry.SetRepository(repository?.Remote, repository?.Branch);
                var locale = LocalizationUtility.GetLocale(repository, options);

                // load configuration from current entry or fallback repository
                var input = new Input(docsetPath, repositoryProvider);
                var configLoader = new ConfigLoader(docsetPath, input, repositoryProvider);

                var configPath = docsetPath;
                var (errors, config) = configLoader.TryLoad(options, extend: false);
                var restoreFallbackResult = RestoreFallbackRepo(config, repository);
                if (restoreFallbackResult != null)
                    repositoryProvider.ConfigFallbackRepository(GetRepository(restoreFallbackResult, bare: true));

                List<Error> fallbackConfigErrors;
                (fallbackConfigErrors, config) = configLoader.Load(options, extend: false);
                errors.AddRange(fallbackConfigErrors);

                // config error log, and return if config has errors
                errorLog.Configure(config);
                if (errorLog.Write(errors))
                    return;

                // restore extend url firstly
                await ParallelUtility.ForEach(
                    config.Extend.Where(UrlUtility.IsHttp),
                    restoreUrl => RestoreFile.Restore(restoreUrl, config));

                // extend the config after the extend url being restored
                var (extendConfigErrors, extendedConfig) = configLoader.Load(options, extend: true);
                errorLog.Write(extendConfigErrors);

                // restore urls except extend url
                var restoreUrls = extendedConfig.GetFileReferences().Where(UrlUtility.IsHttp).ToList();
                await RestoreFile.Restore(restoreUrls, extendedConfig);

                // restore git repos includes dependency repos, theme repo and loc repos
                var restoreDependencyResults = RestoreGit.Restore(extendedConfig, locale, repository, DependencyLockProvider.CreateFromConfig(input, extendedConfig));

                // save dependency lock
                var restoredGitLock = new List<DependencyGitLock>();
                foreach (var restoreResult in restoreDependencyResults.Concat(new[] { restoreFallbackResult }))
                {
                    if (restoreResult != null)
                        restoredGitLock.Add(new DependencyGitLock { Url = restoreResult.Remote, Branch = restoreResult.Branch, Commit = restoreResult.Commit });
                }

                DependencyLockProvider.SaveGitLock(docsetPath, locale, extendedConfig.DependencyLock, restoredGitLock);
            }

            Repository GetRepository(RestoreGitResult restoreGitResult, bool bare)
            {
                var repo = Repository.Create(restoreGitResult.Path, restoreGitResult.Branch, restoreGitResult.Remote, restoreGitResult.Commit, bare);

                return repo;
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

                var restoredResult = RestoreGit.RestoreGitRepo(config, fallbackRemote, new List<(string branch, RestoreGitFlags flags)> { (fallbackBranch, RestoreGitFlags.NoCheckout) }, null);
                Debug.Assert(restoredResult.Count == 1);

                return restoredResult[0];
            }

            return default;
        }
    }
}
