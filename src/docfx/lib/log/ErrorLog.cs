// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build
{
    internal class ErrorLog : ErrorBuilder
    {
        private readonly ErrorBuilder _errors;
        private readonly Config _config;
        private readonly SourceMap? _sourceMap;

        private readonly ErrorSink _errorSink = new ErrorSink();
        private readonly ConcurrentDictionary<FilePath, ErrorSink> _fileSink = new ConcurrentDictionary<FilePath, ErrorSink>();

        public override bool HasError => _errors.HasError;

        public override bool FileHasError(FilePath file) => _fileSink.TryGetValue(file, out var sink) && sink.ErrorCount > 0;

        public ErrorLog(
            ErrorBuilder errors,
            Config config,
            SourceMap? sourceMap = null)
        {
            _errors = errors;
            _config = config;
            _sourceMap = sourceMap;
        }

        public override void Add(Error error)
        {
            /*if (TryGetCustomRule(error, out var customRule))
            {
                error = error.WithCustomRule(customRule);
            }*/

            // todo withCustomRules on config rules here
            // if not match, then
            if (ValidatorExtension != null)
            {
                error = ValidatorExtension.WithCustomRule(error);
            }

            if (error.Level == ErrorLevel.Off)
            {
                return;
            }

            if (_config.WarningsAsErrors && error.Level == ErrorLevel.Warning)
            {
                error = error.WithLevel(ErrorLevel.Error);
            }

            if (error.Source?.File != null && error.Source?.File.Origin == FileOrigin.Fallback)
            {
                if (error.Level == ErrorLevel.Error)
                {
                    Add(Errors.Logging.FallbackError(_config.DefaultLocale));
                }
                return;
            }

            if (error.Source != null)
            {
                error = error.WithOriginalPath(_sourceMap?.GetOriginalFilePath(error.Source.File)?.Path);
            }

            var errorSink = error.Source?.File is null ? _errorSink : _fileSink.GetOrAdd(error.Source.File, _ => new ErrorSink());

            switch (errorSink.Add(error.Source?.File is null ? null : _config, error))
            {
                case ErrorSinkResult.Ok:
                    _errors.Add(error);
                    break;

                case ErrorSinkResult.Exceed when error.Source?.File != null:
                    var maxAllowed = error.Level switch
                    {
                        ErrorLevel.Error => _config.MaxFileErrors,
                        ErrorLevel.Warning => _config.MaxFileWarnings,
                        ErrorLevel.Suggestion => _config.MaxFileSuggestions,
                        ErrorLevel.Info => _config.MaxFileInfos,
                        _ => 0,
                    };
                    _errors.Add(Errors.Logging.ExceedMaxFileErrors(maxAllowed, error.Level, error.Source.File));
                    break;
            }
        }
    }
}
