// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class ErrorLog : ErrorBuilder
    {
        private readonly ErrorBuilder _errors;
        private readonly PathString _docsetBasePath;

        private readonly Scoped<ErrorSink> _errorSink = new();
        private readonly Scoped<ConcurrentDictionary<FilePath, ErrorSink>> _fileSink = new();

        public Config? Config { get; set; }

        public SourceMap? SourceMap { get; set; }

        public MetadataProvider? MetadataProvider { get; set; }

        public CustomRuleProvider? CustomRuleProvider { get; set; }

        public override bool HasError => _errorSink.Value.ErrorCount > 0 || _fileSink.Value.Values.Any(file => file.ErrorCount > 0);

        public override bool FileHasError(FilePath file) => _fileSink.Value.TryGetValue(file, out var sink) && sink.ErrorCount > 0;

        public ErrorLog(ErrorBuilder errors, string workingDirectory, string docsetPath)
        {
            _errors = errors;
            _docsetBasePath = new PathString(Path.GetRelativePath(workingDirectory, docsetPath));
        }

        public override void Add(Error error)
        {
            try
            {
                if (error.Source?.File is FilePath source)
                {
                    var msAuthor = MetadataProvider?.GetMetadata(Null, source).MsAuthor;
                    if (msAuthor != default)
                    {
                        error = error with { MsAuthor = msAuthor };
                    }
                }

                error = CustomRuleProvider?.ApplyCustomRule(error) ?? error;
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                Log.Write(ex);
            }

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
                        Add(Errors.Logging.FallbackError(config.DefaultLocale));
                    }
                    return;
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

            _errors.Add(error);
        }
    }
}
