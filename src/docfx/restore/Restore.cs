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
        public static int Run(string workingDirectory, CommandLineOptions options)
        {
            var docsets = ConfigLoader.FindDocsets(workingDirectory, options);
            if (docsets.Length == 0)
            {
                ErrorLog.PrintError(Errors.Config.ConfigNotFound(workingDirectory));
                return 1;
            }

            var hasError = false;
            Parallel.ForEach(docsets, docset =>
            {
                if (RestoreDocset(docset.docsetPath, docset.outputPath, options))
                {
                    hasError = true;
                }
            });
            return hasError ? 1 : 0;
        }

        public static bool RestoreDocset(string docsetPath, string? outputPath, CommandLineOptions options)
        {
            Config? logConfig = null;

            // Restore has to use Config directly, it cannot depend on Docset,
            // because Docset assumes the repo to physically exist on disk.
            using (var errorLog = new ErrorLog(docsetPath, outputPath, () => logConfig, options.Legacy))
            using (Progress.Start("Restore dependencies"))
            {
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    // load configuration from current entry or fallback repository
                    var configLoader = new ConfigLoader(errorLog);
                    var (errors, config, buildOptions, packageResolver, fileResolver) = configLoader.Load(docsetPath, outputPath, options);
                    if (errorLog.Write(errors))
                        return true;

                    logConfig = config;
                    using (packageResolver)
                    {
                        // download dependencies to disk
                        Parallel.Invoke(
                            () => RestoreFiles(config, fileResolver).GetAwaiter().GetResult(),
                            () => RestorePackages(buildOptions, config, packageResolver));
                    }
                }
                catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
                {
                    return errorLog.Write(dex);
                }
                finally
                {
                    Telemetry.TrackOperationTime("restore", stopwatch.Elapsed);
                    Log.Important($"Restore done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
                    errorLog.PrintSummary();
                }
                return false;
            }
        }

        private static async Task RestoreFiles(Config config, FileResolver fileResolver)
        {
            await ParallelUtility.ForEach(config.GetFileReferences(), fileResolver.Download);
        }

        private static void RestorePackages(BuildOptions buildOptions, Config config, PackageResolver packageResolver)
        {
            ParallelUtility.ForEach(
                GetPackages(config, buildOptions).Distinct(),
                item => packageResolver.DownloadPackage(item.package, item.flags),
                Progress.Update,
                maxDegreeOfParallelism: 8);

            LocalizationUtility.EnsureLocalizationContributionBranch(config, buildOptions.Repository);
        }

        private static IEnumerable<(PackagePath package, PackageFetchOptions flags)> GetPackages(Config config, BuildOptions buildOptions)
        {
            foreach (var (_, package) in config.Dependencies)
            {
                yield return (package, package.PackageFetchOptions);
            }

            if (config.Template.Type == PackageType.Git)
            {
                var theme = LocalizationUtility.GetLocalizedTheme(config.Template, buildOptions);
                yield return (theme, PackageFetchOptions.DepthOne);
            }
        }
    }
}
