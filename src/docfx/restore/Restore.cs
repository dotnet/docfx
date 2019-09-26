// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Restore
    {
        public static Task Run(string workingDirectory, CommandLineOptions options)
        {
            var docsets = ConfigLoader.FindDocsets(workingDirectory, options);
            if (docsets.Length == 0)
            {
                Log.Error(Errors.ConfigNotFound(workingDirectory));
                return Task.CompletedTask;
            }

            return Task.WhenAll(docsets.Select(docset => RestoreDocset(docset.docsetPath, docset.outputPath, options)));
        }

        private static async Task RestoreDocset(string docsetPath, string outputPath, CommandLineOptions options)
        {
            List<Error> errors;
            Config config = null;

            // Restore has to use Config directly, it cannot depend on Docset,
            // because Docset assumes the repo to physically exist on disk.
            using (var errorLog = new ErrorLog(docsetPath, outputPath, () => config, options.Legacy))
            using (Progress.Start("Restore dependencies"))
            {
                var stopwatch = Stopwatch.StartNew();

                try
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
                    (errors, config) = configLoader.TryLoad(options, extend: false);
                    var restoreFallbackResult = RestoreFallbackRepo(config, repository);
                    if (restoreFallbackResult != null)
                        repositoryProvider.ConfigFallbackRepository(Repository.Create(restoreFallbackResult.Path, restoreFallbackResult.Branch, restoreFallbackResult.Remote, restoreFallbackResult.Commit));

                    List<Error> fallbackConfigErrors;
                    (fallbackConfigErrors, config) = configLoader.Load(options, extend: false);
                    errors.AddRange(fallbackConfigErrors);

                    // config error log, and return if config has errors
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
                catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
                {
                    Log.Write(dex);
                    errorLog.Write(dex.Error, isException: true);
                }
                finally
                {
                    Telemetry.TrackOperationTime("restore", stopwatch.Elapsed);
                    Log.Important($"Restore '{config?.Name}' done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
                    errorLog.PrintSummary();
                }
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
