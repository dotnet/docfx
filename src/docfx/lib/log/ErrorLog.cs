// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build
{
    internal sealed class ErrorLog : IDisposable
    {
        private readonly object _outputLock = new object();

        private Lazy<TextWriter> _output;
        private Config? _config;
        private SourceMap? _sourceMap;
        private Dictionary<string, CustomRule> _customRules = new Dictionary<string, CustomRule>();

        private ErrorSink _errorSink = new ErrorSink();
        private ConcurrentDictionary<FilePath, ErrorSink> _fileSink = new ConcurrentDictionary<FilePath, ErrorSink>();

        public int ErrorCount => _errorSink.ErrorCount + _fileSink.Values.Sum(sink => sink.ErrorCount);

        public int WarningCount => _errorSink.WarningCount + _fileSink.Values.Sum(sink => sink.WarningCount);

        public int SuggestionCount => _errorSink.SuggestionCount + _fileSink.Values.Sum(sink => sink.SuggestionCount);

        public bool HasError(FilePath file) => _fileSink.TryGetValue(file, out var sink) && sink.ErrorCount > 0;

        public ErrorLog(string? outputPath = null)
        {
            _output = new Lazy<TextWriter>(() => outputPath is null ? TextWriter.Null : CreateOutput(outputPath));
        }

        public void Configure(Config config, string outputPath, SourceMap? sourceMap, Dictionary<string, ValidationRules>? contentValidationRules = null)
        {
            _config = config;
            _sourceMap = sourceMap;
            _customRules = MergeCustomRules(config, contentValidationRules);

            lock (_outputLock)
            {
                if (_output.IsValueCreated)
                {
                    _output.Value.Flush();
                    _output.Value.Dispose();
                }
                _output = new Lazy<TextWriter>(() => CreateOutput(outputPath));
            }
        }

        public bool Write(IEnumerable<Error> errors)
        {
            var hasErrors = false;
            foreach (var error in errors)
            {
                if (Write(error))
                {
                    hasErrors = true;
                }
            }
            return hasErrors;
        }

        public bool Write(IEnumerable<DocfxException> exceptions)
        {
            var hasErrors = false;
            foreach (var exception in exceptions)
            {
                Log.Write(exception);
                if (Write(exception.Error, exception.OverwriteLevel))
                {
                    hasErrors = true;
                }
            }
            return hasErrors;
        }

        public bool Write(Error error, ErrorLevel? overwriteLevel = null)
        {
            var config = _config;
            if (config != null && _customRules.TryGetValue(error.Code, out var customRule))
            {
                error = error.WithCustomRule(customRule);
            }

            var level = overwriteLevel ?? error.Level;
            if (level == ErrorLevel.Off)
            {
                return false;
            }

            if (config != null && config.WarningsAsErrors && level == ErrorLevel.Warning)
            {
                level = ErrorLevel.Error;
            }

            if (config != null && error.Source?.File != null && error.Source?.File.Origin == FileOrigin.Fallback)
            {
                if (level == ErrorLevel.Error)
                {
                    return Write(Errors.Logging.FallbackError(config.DefaultLocale));
                }
                return false;
            }

            var errorSink = error.Source?.File is null ? _errorSink : _fileSink.GetOrAdd(error.Source.File, _ => new ErrorSink());

            switch (errorSink.Add(error.Source?.File is null ? null : config, error, level))
            {
                case ErrorSinkResult.Ok:
                    WriteCore(error, level);
                    break;

                case ErrorSinkResult.Exceed when error.Source?.File != null && config != null:
                    var maxAllowed = level switch
                    {
                        ErrorLevel.Error => config.MaxFileErrors,
                        ErrorLevel.Warning => config.MaxFileWarnings,
                        ErrorLevel.Suggestion => config.MaxFileSuggestions,
                        ErrorLevel.Info => config.MaxFileInfos,
                        _ => 0,
                    };
                    WriteCore(Errors.Logging.ExceedMaxFileErrors(maxAllowed, level, error.Source.File), ErrorLevel.Info);
                    break;
            }

            return level == ErrorLevel.Error;
        }

        [SuppressMessage("Reliability", "CA2002", Justification = "Lock Console.Out")]
        public void PrintSummary()
        {
            lock (Console.Out)
            {
                if (ErrorCount > 0 || WarningCount > 0 || SuggestionCount > 0)
                {
                    Console.ForegroundColor = ErrorCount > 0 ? ConsoleColor.Red
                                            : WarningCount > 0 ? ConsoleColor.Yellow
                                            : ConsoleColor.Magenta;
                    Console.WriteLine();
                    Console.WriteLine($"  {ErrorCount} Error(s), {WarningCount} Warning(s), {SuggestionCount} Suggestion(s)");
                }

                Console.ResetColor();
            }
        }

        [SuppressMessage("Reliability", "CA2002", Justification = "Lock Console.Out")]
        public static void PrintError(Error error, ErrorLevel? level = null)
        {
            lock (Console.Out)
            {
                var errorLevel = level ?? error.Level;
                var output = errorLevel == ErrorLevel.Error ? Console.Error : Console.Out;
                Console.ForegroundColor = GetColor(errorLevel);
                output.Write(error.Code + " ");
                Console.ResetColor();

                if (error.Source != null)
                {
                    output.WriteLine($"./{error.Source.File}({error.Source.Line},{error.Source.Column}): {error.Message}");
                }
                else
                {
                    output.WriteLine(error.Message);
                }
            }
        }

        public static void PrintErrors(List<Error> errors)
        {
            foreach (var error in errors)
            {
                PrintError(error);
            }
        }

        public void Dispose()
        {
            lock (_outputLock)
            {
                if (_output.IsValueCreated)
                {
                    _output.Value.Dispose();
                }
            }
        }

        private void WriteCore(Error error, ErrorLevel level)
        {
            Telemetry.TrackErrorCount(error.Code, level, error.Name);

            if (_output != null)
            {
                lock (_outputLock)
                {
                    _output.Value.WriteLine(error.ToString(level, _sourceMap));
                }
            }

            PrintError(error, level);
        }

        private TextWriter CreateOutput(string outputPath)
        {
            // add default build log file output path
            var outputFilePath = Path.GetFullPath(Path.Combine(outputPath, ".errors.log"));

            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

            return File.AppendText(outputFilePath);
        }

        private static ConsoleColor GetColor(ErrorLevel level)
        {
            return level switch
            {
                ErrorLevel.Error => ConsoleColor.Red,
                ErrorLevel.Warning => ConsoleColor.Yellow,
                ErrorLevel.Suggestion => ConsoleColor.Magenta,
                ErrorLevel.Info => ConsoleColor.DarkGray,
                _ => ConsoleColor.DarkGray,
            };
        }

        private Dictionary<string, CustomRule> MergeCustomRules(Config? config, Dictionary<string, ValidationRules>? validationRules)
        {
            var customRules = config != null ? new Dictionary<string, CustomRule>(config.CustomRules) : new Dictionary<string, CustomRule>();

            if (validationRules == null)
            {
                return customRules;
            }

            foreach (var validationRule in validationRules.SelectMany(rules => rules.Value.Rules).Where(rule => rule.PullRequestOnly))
            {
                if (config != null && customRules.TryGetValue(validationRule.Code, out var customRule))
                {
                    customRules[validationRule.Code] = new CustomRule(
                            customRule.Severity,
                            customRule.Code,
                            customRule.AdditionalMessage,
                            customRule.CanonicalVersionOnly,
                            validationRule.PullRequestOnly);
                }
                else
                {
                    customRules.Add(validationRule.Code, new CustomRule(null, null, null, false, validationRule.PullRequestOnly));
                }
            }
            return customRules;
        }
    }
}
