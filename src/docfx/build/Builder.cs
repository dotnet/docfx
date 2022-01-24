// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class Builder
{
    private readonly ScopedErrorBuilder _errors = new();
    private readonly ScopedProgressReporter _progressReporter = new();
    private readonly CommandLineOptions _options;
    private readonly Watch<DocsetBuilder[]> _docsets;
    private readonly Package _package;
    private readonly CredentialProvider? _getCredential;

    public Builder(CommandLineOptions options, Package package, CredentialProvider? getCredential = null)
    {
        _options = options;
        _package = package;
        _getCredential = getCredential;
        _docsets = new(LoadDocsets);
    }

    public static bool Run(CommandLineOptions options, Package? package = null)
    {
        using (Watcher.Disable())
        {
            using var errors = new ErrorWriter(options.Log);

            if (options.Continue)
            {
                // Apply templates.
                ContinueBuild.Run(errors, options);
            }
            else
            {
                var files = options.File?.Select(Path.GetFullPath).ToArray();

                package ??= new LocalPackage(options.WorkingDirectory);

                new Builder(options, package).Build(errors, new ConsoleProgressReporter(), files);
            }

            errors.PrintSummary();
            return errors.HasError;
        }
    }

    public void Build(ErrorBuilder errors, IProgress<string> progressReporter, string[]? files = null)
    {
        if (files?.Length == 0)
        {
            return;
        }

        using (Watcher.BeginScope())
        using (_errors.BeginScope(errors))
        using (_progressReporter.BeginScope(progressReporter))
        {
            try
            {
                Parallel.ForEach(
                    _docsets.Value,
                    docset => docset.Build(files is null ? null : Array.ConvertAll(files, path => GetPathToDocset(docset, path))));
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                _errors.AddRange(dex);
            }
        }
    }

    private DocsetBuilder[] LoadDocsets()
    {
        _progressReporter.Report("Loading docsets...");

        // load and trace entry repository
        var repository = Repository.Create(_package.BasePath);
        Telemetry.SetRepository(repository?.Url, repository?.Branch);

        var docsets = ConfigLoader.FindDocsets(_errors, _package, _options, repository);
        if (docsets.Length == 0)
        {
            _errors.Add(Errors.Config.ConfigNotFound(_options.WorkingDirectory));
        }

        return (from docset in docsets
                let item = DocsetBuilder.Create(
                    _errors,
                    repository,
                    docset.docsetPath,
                    docset.outputPath,
                    _package.CreateSubPackage(docset.docsetPath),
                    _options,
                    _progressReporter,
                    _getCredential)
                where item != null
                select item).ToArray();
    }

    private string GetPathToDocset(DocsetBuilder docset, string file)
    {
        return Path.GetRelativePath(docset.BuildOptions.DocsetPath, Path.Combine(_options.WorkingDirectory, file));
    }
}
