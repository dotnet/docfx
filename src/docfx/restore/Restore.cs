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

        var publishRepositoryUrl = repository?.Url ?? string.Empty;
        var publishRepositoryBranch = repository?.Branch ?? string.Empty;
        if (!string.IsNullOrEmpty(EnvironmentVariable.PublishRepositoryUrl))
        {
            publishRepositoryUrl = EnvironmentVariable.PublishRepositoryUrl;
            publishRepositoryBranch = "main";
        }

        var docsets = ConfigLoader.FindDocsets(errors, package, options, repository);
        if (docsets.Length == 0)
        {
            errors.Add(Errors.Config.ConfigNotFound(options.WorkingDirectory));
            return errors.HasError;
        }

        Parallel.ForEach(
            docsets,
            docset => RestoreDocset(
                errors, repository, publishRepositoryUrl, publishRepositoryBranch, docset.docsetPath, docset.outputPath, options, FetchOptions.Latest));

        errors.PrintSummary();
        return errors.HasError;
    }

    public static void RestoreDocset(
        ErrorBuilder errors,
        Repository? repository,
        string publishRepositoryUrl,
        string publishRepositoryBranch,
        string docsetPath,
        string? outputPath,
        CommandLineOptions options,
        FetchOptions fetchOptions)
    {
        var errorLog = new ErrorLog(errors, options.WorkingDirectory, docsetPath);

        try
        {
            // load configuration from current entry or fallback repository
            var localPackage = new LocalPackage(Path.Combine(options.WorkingDirectory, docsetPath));
            var (config, buildOptions, packageResolver, fileResolver, _) = ConfigLoader.Load(
                errorLog, repository, publishRepositoryUrl, publishRepositoryBranch, docsetPath, outputPath, options, fetchOptions, localPackage);

            if (errorLog.HasError)
            {
                return;
            }

            errorLog.Config = config;
            RestoreDocset(errorLog, config, packageResolver, fileResolver);
        }
        catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
        {
            errorLog.AddRange(dex);
        }
    }

    public static void RestoreDocset(
        ErrorBuilder errors, Config config, PackageResolver packageResolver, FileResolver fileResolver)
    {
        // download dependencies to disk
        Parallel.Invoke(
            () => RestoreFiles(errors, config, fileResolver),
            () => RestorePackages(errors, config, packageResolver));
    }

    private static void RestoreFiles(ErrorBuilder errors, Config config, FileResolver fileResolver)
    {
        using var scope = Progress.Start("Restoring files");
        ParallelUtility.ForEach(scope, errors, config.GetFileReferences(), fileResolver.Download);
    }

    private static void RestorePackages(ErrorBuilder errors, Config config, PackageResolver packageResolver)
    {
        using var scope = Progress.Start("Restoring packages");
        ParallelUtility.ForEach(
            scope,
            errors,
            GetPackages(config).Distinct(),
            item => packageResolver.DownloadPackage(item.package, item.flags));
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
