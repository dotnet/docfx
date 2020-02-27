// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class Restore
    {
        public static int Run(string workingDirectory, CommandLineOptions options)
        {
            options.UseCache = TestQuirks.RestoreUseCache?.Invoke() ?? options.UseCache;

            var docsets = ConfigLoader.FindDocsets(workingDirectory, options);
            if (docsets.Length == 0)
            {
                ErrorLog.PrintError(Errors.ConfigNotFound(workingDirectory));
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

        private static bool RestoreDocset(string docsetPath, string? outputPath, CommandLineOptions options)
        {
            List<Error> errors;
            Config? config = null;

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
                    var locale = LocalizationUtility.GetLocale(repository);

                    // load configuration from current entry or fallback repository
                    var configLoader = new ConfigLoader(repository, errorLog);
                    (errors, config) = configLoader.Load(docsetPath, locale, options);
                    if (errorLog.Write(errors))
                        return true;

                    // download dependencies to disk
                    Parallel.Invoke(
                        () => RestoreFiles(docsetPath, config, errorLog, options.FetchOptions).GetAwaiter().GetResult(),
                        () => RestorePackages(docsetPath, config, locale, repository, options.FetchOptions));
                }
                catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
                {
                    return errorLog.Write(dex);
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

        private static async Task RestoreFiles(string docsetPath, Config config, ErrorLog errorLog, FetchOptions fetchOptions)
        {
            var credentialProvider = config.GetCredentialProvider();
            var fileResolver = new FileResolver(docsetPath, credentialProvider, new OpsConfigAdapter(errorLog, credentialProvider), fetchOptions);
            await ParallelUtility.ForEach(config.GetFileReferences(), fileResolver.Download);
        }

        private static void RestorePackages(string docsetPath, Config config, string locale, Repository? repository, FetchOptions fetchOptions)
        {
            using var packageResolver = new PackageResolver(docsetPath, config, fetchOptions);
            ParallelUtility.ForEach(
                GetPackages(config, locale, repository).Distinct(),
                item => packageResolver.DownloadPackage(item.package, item.flags),
                Progress.Update,
                maxDegreeOfParallelism: 8);

            EnsureLocalizationContributionBranch(config, repository);
        }

        private static void EnsureLocalizationContributionBranch(Config config, Repository? repository)
        {
            // When building the live-sxs branch of a loc repo, only live-sxs branch is cloned,
            // this clone process is managed outside of build, so we need to explicitly fetch the history of live branch
            // here to generate the correct contributor list.
            if (repository != null && LocalizationUtility.TryGetContributionBranch(repository.Branch, out var contributionBranch))
            {
                try
                {
                    GitUtility.Fetch(config, repository.Path, repository.Remote, $"+{contributionBranch}:{contributionBranch}", "--update-head-ok");
                }
                catch (InvalidOperationException ex)
                {
                    throw Errors.CommittishNotFound(repository.Remote, contributionBranch).ToException(ex);
                }
            }
        }

        private static IEnumerable<(PackagePath package, PackageFetchOptions flags)> GetPackages(
            Config config, string locale, Repository? repository)
        {
            foreach (var (_, package) in config.Dependencies)
            {
                yield return (package, package.PackageFetchOptions);
            }

            if (config.Template.Type == PackageType.Git)
            {
                var theme = LocalizationUtility.GetLocalizedTheme(config.Template, locale, config.DefaultLocale);
                yield return (theme, PackageFetchOptions.DepthOne);
            }

            foreach (var item in GetLocalizationPackages(config, locale, repository))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Get source repository or localized repository
        /// </summary>
        private static IEnumerable<(PackagePath package, PackageFetchOptions flags)> GetLocalizationPackages(
            Config config, string locale, Repository? repository)
        {
            if (string.IsNullOrEmpty(locale))
            {
                yield break;
            }

            if (string.Equals(locale, config.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            if (repository is null || string.IsNullOrEmpty(repository.Remote))
            {
                yield break;
            }

            if (LocalizationUtility.TryGetFallbackRepository(repository.Remote, repository.Branch, out var fallbackRemote, out var fallbackBranch, out _))
            {
                // fallback to master
                yield return (new PackagePath(fallbackRemote, fallbackBranch), PackageFetchOptions.IgnoreError | PackageFetchOptions.None);
                yield return (new PackagePath(fallbackRemote, "master"), PackageFetchOptions.IgnoreError | PackageFetchOptions.None);
                yield break;
            }
        }
    }
}
