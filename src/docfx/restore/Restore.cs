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
            var (errors, docsets) = ConfigLoader.FindDocsets(workingDirectory, options);
            ErrorLog.PrintErrors(errors);
            if (docsets.Length == 0)
            {
                ErrorLog.PrintError(Errors.Config.ConfigNotFound(workingDirectory));
                return 1;
            }

            var hasError = false;
            Parallel.ForEach(docsets, docset =>
            {
                if (RestoreDocset(docset.docsetPath, docset.outputPath, options, FetchOptions.Latest))
                {
                    hasError = true;
                }
            });
            return hasError ? 1 : 0;
        }

        public static bool RestoreDocset(string docsetPath, string? outputPath, CommandLineOptions options, FetchOptions fetchOptions)
        {
            var stopwatch = Stopwatch.StartNew();

            using var disposables = new DisposableCollector();
            using var errorLog = new ErrorLog(outputPath);

            try
            {
                // load configuration from current entry or fallback repository
                var configLoader = new ConfigLoader(errorLog);
                var (errors, config, buildOptions, packageResolver, fileResolver) = configLoader.Load(disposables, docsetPath, outputPath, options, fetchOptions);
                if (errorLog.Write(errors))
                {
                    return true;
                }

                errorLog.Configure(config, buildOptions.OutputPath, null);

                // download dependencies to disk
                Parallel.Invoke(
                    () => RestoreFiles(errorLog, config, fileResolver).GetAwaiter().GetResult(),
                    () => RestorePackages(errorLog, buildOptions, config, packageResolver));
                return errorLog.ErrorCount > 0;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errorLog.Write(dex);
                return errorLog.ErrorCount > 0;
            }
            finally
            {
                Telemetry.TrackOperationTime("restore", stopwatch.Elapsed);
                Log.Important($"Restore done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
                errorLog.PrintSummary();
            }
        }

        private static async Task RestoreFiles(ErrorLog errorLog, Config config, FileResolver fileResolver)
        {
            await ParallelUtility.ForEach(errorLog, config.GetFileReferences(), fileResolver.Download);
        }

        private static void RestorePackages(ErrorLog errorLog, BuildOptions buildOptions, Config config, PackageResolver packageResolver)
        {
            ParallelUtility.ForEach(
                errorLog,
                GetPackages(config).Distinct(),
                item => packageResolver.DownloadPackage(item.package, item.flags));

            LocalizationUtility.EnsureLocalizationContributionBranch(config, buildOptions.Repository);
        }

        private static IEnumerable<(PackagePath package, PackageFetchOptions flags)> GetPackages(Config config)
        {
            foreach (var (_, package) in config.Dependencies)
            {
                yield return (package, package.PackageFetchOptions);
            }

            if (config.Template.Type == PackageType.Git)
            {
                yield return (config.Template, PackageFetchOptions.DepthOne);
            }
        }
    }
}
