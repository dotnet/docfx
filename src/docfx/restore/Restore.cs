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
        public static async Task<int> Run(string workingDirectory, CommandLineOptions options)
        {
            var docsets = ConfigLoader.FindDocsets(workingDirectory, options);
            if (docsets.Length == 0)
            {
                ErrorLog.PrintError(Errors.ConfigNotFound(workingDirectory));
                return 1;
            }

            var result = await Task.WhenAll(docsets.Select(docset => RestoreDocset(docset.docsetPath, docset.outputPath, options)));
            return result.Any(hasError => hasError) ? 1 : 0;
        }

        private static async Task<bool> RestoreDocset(string docsetPath, string outputPath, CommandLineOptions options)
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
                    var repository = Repository.Create(docsetPath);
                    Telemetry.SetRepository(repository?.Remote, repository?.Branch);
                    var locale = LocalizationUtility.GetLocale(repository, options);

                    // load configuration from current entry or fallback repository
                    var configLoader = new ConfigLoader(repository, errorLog);

                    (errors, config) = configLoader.Load(docsetPath, locale, options);
                    if (errorLog.Write(errors))
                        return true;

                    var credentialProvider = config.GetCredentialProvider();
                    var fileResolver = new FileResolver(docsetPath, credentialProvider, new OpsConfigAdapter(errorLog, credentialProvider));
                    await ParallelUtility.ForEach(config.GetFileReferences(), fileResolver.Download);

                    // restore git repos includes dependency repos, theme repo and loc repos
                    var restoreDependencyResults = RestoreGit.Restore(config, locale, repository, DependencyLockProvider.CreateFromConfig(docsetPath, config));

                    // save dependency lock
                    var restoredGitLock = new List<DependencyGitLock>();
                    foreach (var restoreResult in restoreDependencyResults)
                    {
                        if (restoreResult != null)
                            restoredGitLock.Add(new DependencyGitLock { Url = restoreResult.Remote, Branch = restoreResult.Branch, Commit = restoreResult.Commit });
                    }

                    DependencyLockProvider.SaveGitLock(docsetPath, locale, config.DependencyLock, restoredGitLock);
                }
                catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
                {
                    Log.Write(dex);
                    return errorLog.Write(dex.Error);
                }
                finally
                {
                    Telemetry.TrackOperationTime("restore", stopwatch.Elapsed);
                    Log.Important($"Restore '{config?.Name}' done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
                    errorLog.PrintSummary();
                }
                return false;
            }
        }
    }
}
