// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal class ErrorLog : ErrorBuilder
{
    private readonly ErrorBuilder _errors;
    private readonly PathString _docsetBasePath;
    private readonly PathString _workingDirectory;

    private readonly Scoped<ErrorSink> _errorSink = new();
    private readonly Scoped<ConcurrentDictionary<FilePath, ErrorSink>> _fileSink = new();

    public Config? Config { get; set; }

    public SourceMap? SourceMap { get; set; }

    public MetadataProvider? MetadataProvider { get; set; }

    public CustomRuleProvider? CustomRuleProvider { get; set; }

    public ContributionProvider? ContributionProvider { get; set; }

    public override bool HasError => _errorSink.Value.ErrorCount > 0 || _fileSink.Value.Values.Any(file => file.ErrorCount > 0);

    public override bool FileHasError(FilePath file) => _fileSink.Value.TryGetValue(file, out var sink) && sink.ErrorCount > 0;

    public ErrorLog(ErrorBuilder errors, string workingDirectory, string docsetPath)
    {
        _errors = errors;
        _workingDirectory = new PathString(workingDirectory);
        _docsetBasePath = new PathString(Path.GetRelativePath(workingDirectory, docsetPath));
    }

    public override void Add(Error error)
    {
        if (error.Source?.File is FilePath source)
        {
            try
            {
                if (error.AdditionalErrorInfo == null)
                {
                    var metadata = MetadataProvider?.GetMetadata(Null, source);
                    if (new[]
                    {
                        metadata?.MsAuthor,
                        metadata?.MsProd,
                        metadata?.MsTechnology,
                        metadata?.MsService,
                        metadata?.MsSubservice,
                        metadata?.MsTopic,
                    }.Any(value => !string.IsNullOrEmpty(value)))
                    {
                        error = error with
                        {
                            AdditionalErrorInfo = new AdditionalErrorInfo(
                                metadata?.MsAuthor,
                                metadata?.MsProd,
                                metadata?.MsTechnology,
                                metadata?.MsService,
                                metadata?.MsSubservice,
                                metadata?.MsTopic),
                        };
                    }
                }
            }
            catch
            {
            }
        }

        error = CustomRuleProvider?.ApplyCustomRule(error) ?? error;

        if (error.Level == ErrorLevel.Off)
        {
            return;
        }

        if (error.Source != null && SourceMap != null)
        {
            error = error with { OriginalPath = SourceMap.GetOriginalFilePath(error.Source.File)?.Path };
        }

        var config = Config;
        if (config != null)
        {
            if (config.WarningsAsErrors && error.Level == ErrorLevel.Warning)
            {
                error = error with { Level = ErrorLevel.Error };
            }

            if (error.Source?.File != null && error.Source?.File.Origin == FileOrigin.Fallback)
            {
                if (error.Level == ErrorLevel.Error)
                {
                    Log.Write(error.ToString());
                    Add(Errors.Logging.FallbackError(config.DefaultLocale));
                }
                return;
            }

            if (config.DocumentUrls.TryGetValue(error.Code, out var documentUrl))
            {
                error = error with { DocumentUrl = documentUrl };
            }
        }

        Watcher.Write(() =>
        {
            var errorSink = error.Source?.File is null ? _errorSink.Value : _fileSink.Value.GetOrAdd(error.Source.File, _ => new ErrorSink());

            switch (errorSink.Add(error.Source?.File is null ? null : config, error))
            {
                case ErrorSinkResult.Ok:
                    AddError(error);
                    break;

                case ErrorSinkResult.Exceed when error.Source?.File != null && config != null:
                    var maxAllowed = error.Level switch
                    {
                        ErrorLevel.Error => config.MaxFileErrors,
                        ErrorLevel.Warning => config.MaxFileWarnings,
                        ErrorLevel.Suggestion => config.MaxFileSuggestions,
                        ErrorLevel.Info => config.MaxFileInfos,
                        _ => 0,
                    };
                    AddError(Errors.Logging.ExceedMaxFileErrors(maxAllowed, error.Level, error.Source.File));
                    break;
            }
        });
    }

    private void AddError(Error error)
    {
        var originalFilePath = error.Source?.File;

        // Convert from path relative to docset to path relative to working directory
        if (!_docsetBasePath.IsDefault)
        {
            if (error.Source != null)
            {
                var path = _docsetBasePath.Concat(error.Source.File.Path);
                error = error with { Source = error.Source with { File = error.Source.File with { Path = path } } };
            }

            if (error.OriginalPath != null)
            {
                error = error with { OriginalPath = _docsetBasePath.Concat(error.OriginalPath.Value) };
            }
        }

        if (originalFilePath != null)
        {
            // The original FilePath is used as key to fetch ContributionProvider git url cache
            // and meets the requirement of https://github.com/dotnet/docfx/blob/c8cb790043ae5b93173f3e28dafc28bf7f305d48/src/docfx/build/context/Input.cs#L292
            (_, string? originalContentGitUrl, _) = ContributionProvider.GetGitUrl(originalFilePath);
            error = error with { SourceUrl = originalContentGitUrl };
        }
        _errors.Add(error);
    }
}
