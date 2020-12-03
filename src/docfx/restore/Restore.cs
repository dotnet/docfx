// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Restore
    {
        public static bool Run(string workingDirectory, CommandLineOptions options)
        {
            var stopwatch = Stopwatch.StartNew();
            using var errors = new ErrorWriter(options.Log);

            var docsets = ConfigLoader.FindDocsets(errors, workingDirectory, new LocalPackage(workingDirectory), options);
            if (docsets.Length == 0)
            {
                errors.Add(Errors.Config.ConfigNotFound(workingDirectory));
                return errors.HasError;
            }

            Parallel.ForEach(docsets, docset =>
            {
                RestoreDocset(errors, workingDirectory, docset.docsetPath, docset.outputPath, options, FetchOptions.Latest);
            });

            Telemetry.TrackOperationTime("restore", stopwatch.Elapsed);
            Log.Important($"Restore done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
            errors.PrintSummary();
            return errors.HasError;
        }

        public static void RestoreDocset(
            ErrorBuilder errors, string workingDirectory, string docsetPath, string? outputPath, CommandLineOptions options, FetchOptions fetchOptions)
        {
            errors = errors.WithDocsetPath(workingDirectory, docsetPath);

            try
            {
                // load configuration from current entry or fallback repository
                var (config, buildOptions, packageResolver, fileResolver, _) = ConfigLoader.Load(
                    errors, docsetPath, outputPath, options, fetchOptions, new LocalPackage(Path.Combine(workingDirectory, docsetPath)));

                if (errors.HasError)
                {
                    return;
                }

                errors = new ErrorLog(errors, config);
                RestoreDocset(errors, config, buildOptions, packageResolver, fileResolver);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                errors.AddRange(dex);
            }
        }

        public static void RestoreDocset(
            ErrorBuilder errors, Config config, BuildOptions buildOptions, PackageResolver packageResolver, FileResolver fileResolver)
        {
            // download dependencies to disk
            Parallel.Invoke(
                () => RestoreFiles(errors, config, fileResolver),
                () => RestorePackages(errors, buildOptions, config, packageResolver));
        }

        private static void RestoreFiles(ErrorBuilder errors, Config config, FileResolver fileResolver)
        {
            ParallelUtility.ForEach(errors, config.GetFileReferences(), fileResolver.Download);
        }

        private static void RestorePackages(ErrorBuilder errors, BuildOptions buildOptions, Config config, PackageResolver packageResolver)
        {
            ParallelUtility.ForEach(
                errors,
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
