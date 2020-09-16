// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private readonly Dictionary<string, SourceInfo<CustomRule>> _customRules = new Dictionary<string, SourceInfo<CustomRule>>();

        private readonly ErrorSink _errorSink = new ErrorSink();
        private readonly ConcurrentDictionary<FilePath, ErrorSink> _fileSink = new ConcurrentDictionary<FilePath, ErrorSink>();

        public override bool HasError => _errors.HasError;

        public override bool FileHasError(FilePath file) => _fileSink.TryGetValue(file, out var sink) && sink.ErrorCount > 0;

        public ErrorLog(
            ErrorBuilder errors,
            Config config,
            SourceMap? sourceMap = null,
            Dictionary<string, ValidationRules>? contentValidationRules = null)
        {
            _errors = errors;
            _config = config;
            _sourceMap = sourceMap;
            _customRules = MergeCustomRules(config, contentValidationRules);
        }

        public override void Add(Error error)
        {
            if (_customRules.TryGetValue(error.Code, out var customRule))
            {
                error = error.WithCustomRule(customRule);
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
                error = error.WithOriginalPath(_sourceMap?.GetOriginalFilePath(error.Source.File));
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

        private Dictionary<string, SourceInfo<CustomRule>> MergeCustomRules(Config? config, Dictionary<string, ValidationRules>? validationRules)
        {
            var customRules = config != null ? new Dictionary<string, SourceInfo<CustomRule>>(_config.Rules) : new Dictionary<string, SourceInfo<CustomRule>>();

            if (validationRules == null)
            {
                return customRules;
            }

            foreach (var validationRule in validationRules.SelectMany(rules => rules.Value.Rules).Where(rule => !rule.DocfxOverride))
            {
                if (customRules.ContainsKey(validationRule.Code))
                {
                    Add(Errors.Logging.OverrideNotAllowed(validationRule.Code, customRules[validationRule.Code].Source));
                    customRules.Remove(validationRule.Code);
                }
            }
            foreach (var validationRule in validationRules.SelectMany(rules => rules.Value.Rules).Where(rule => rule.PullRequestOnly))
            {
                if (customRules.TryGetValue(validationRule.Code, out var customRule))
                {
                    customRules[validationRule.Code] = new SourceInfo<CustomRule>(
                        new CustomRule(
                            customRule.Value.Severity,
                            customRule.Value.Code,
                            customRule.Value.AdditionalMessage,
                            customRule.Value.CanonicalVersionOnly,
                            validationRule.PullRequestOnly), customRule.Source);
                }
                else
                {
                    customRules.Add(validationRule.Code, new SourceInfo<CustomRule>(new CustomRule(null, null, null, false, validationRule.PullRequestOnly)));
                }
            }
            return customRules;
        }
    }
}
