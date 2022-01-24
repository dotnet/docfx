// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal static class Restore
{
    public static bool Run(CommandLineOptions options)
    {
        using var errors = new ErrorWriter(options.Log);

        var package = new LocalPackage(options.WorkingDirectory);
        var repository = Repository.Create(package.BasePath);
        Telemetry.SetRepository(repository?.Url, repository?.Branch);

        var docsets = ConfigLoader.FindDocsets(errors, package, options, repository);
        if (docsets.Length == 0)
        {
            errors.Add(Errors.Config.ConfigNotFound(options.WorkingDirectory));
            return errors.HasError;
        }

        Parallel.ForEach(docsets, docset => RestoreDocset(errors, repository, docset.docsetPath, docset.outputPath, options, FetchOptions.Latest));

        errors.PrintSummary();
        return errors.HasError;
    }

    public static void RestoreDocset(
        ErrorBuilder errors, Repository? repository, string docsetPath, string? outputPath, CommandLineOptions options, FetchOptions fetchOptions)
    {
        var errorLog = new ErrorLog(errors, options.WorkingDirectory, docsetPath);

        try
        {
            // load configuration from current entry or fallback repository
            var (config, buildOptions, packageResolver, fileResolver, _) = ConfigLoader.Load(
                errorLog, repository, docsetPath, outputPath, options, fetchOptions, new LocalPackage(Path.Combine(options.WorkingDirectory, docsetPath)));

            if (errorLog.HasError)
            {
                return;
            }

            errorLog.Config = config;
            RestoreDocset(errorLog, config, buildOptions, packageResolver, fileResolver);
        }
        catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
        {
            errorLog.AddRange(dex);
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
        using var scope = Progress.Start("Restoring files");
        ParallelUtility.ForEach(scope, errors, config.GetFileReferences(), fileResolver.Download);
    }

    private static void RestorePackages(ErrorBuilder errors, BuildOptions buildOptions, Config config, PackageResolver packageResolver)
    {
        using (var scope = Progress.Start("Restoring packages"))
        {
            ParallelUtility.ForEach(
                scope,
                errors,
                GetPackages(config).Distinct(),
                item => packageResolver.DownloadPackage(item.package, item.flags));
        }
        LocalizationUtility.EnsureLocalizationContributionBranch(config.Secrets, buildOptions.Repository);
    }

    private static IEnumerable<(PackagePath package, PackageFetchOptions flags)> GetPackages(Config config)
    {
        foreach (var (_, package) in config.Dependencies)
        {
            yield return (package, package.PackageFetchOptions);
        }

        if (config.Template.Type == PackageType.Git)
        {
            yield return (config.Template, PackageFetchOptions.DepthOne | PackageFetchOptions.IgnoreBranchFallbackError);
        }
    }
}
