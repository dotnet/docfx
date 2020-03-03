// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal sealed class ErrorLog : IDisposable
    {
        private readonly bool _legacy;
        private readonly object _outputLock = new object();
        private readonly Func<Config?> _config;

        private readonly ConcurrentHashSet<Error> _errors = new ConcurrentHashSet<Error>(Error.Comparer);
        private readonly Lazy<TextWriter> _output;

        private int _errorCount;
        private int _warningCount;
        private int _suggestionCount;

        private int _maxExceeded;

        public int ErrorCount => _errorCount;

        public int WarningCount => _warningCount;

        public int SuggestionCount => _suggestionCount;

        public ErrorLog(string docsetPath, string? outputPath, Func<Config?> config, bool legacy = false)
        {
            _config = config;
            _legacy = legacy;
            _output = new Lazy<TextWriter>(() =>
            {
                if (string.IsNullOrEmpty(outputPath))
                {
                    var conf = _config();
                    if (conf == null)
                    {
                        return TextWriter.Null;
                    }

                    outputPath = Path.Combine(docsetPath, conf.OutputPath);
                }

                // add default build log file output path
                var outputFilePath = Path.GetFullPath(Path.Combine(outputPath, ".errors.log"));

                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                return File.AppendText(outputFilePath);
            });
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
            var config = _config();

            if (config != null && config.CustomErrors.TryGetValue(error.Code, out var customError))
            {
                error = error.WithCustomError(customError);
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

            if (error.Code == "circular-reference" || error.Code == "include-not-found")
            {
                level = ErrorLevel.Error;
            }

            if (config != null && error.FilePath != null && error.FilePath.Origin == FileOrigin.Fallback)
            {
                if (level == ErrorLevel.Error)
                {
                    return Write(Errors.FallbackError(config.DefaultLocale));
                }
                return false;
            }

            if (ExceedMaxErrors(config, level))
            {
                if (Interlocked.Exchange(ref _maxExceeded, 1) == 0)
                {
                    WriteCore(Errors.ExceedMaxErrors(GetMaxCount(config, level), level), level);
                }
            }
            else if (_errors.TryAdd(error) && !IncrementExceedMaxErrors(config, level))
            {
                WriteCore(error, level);
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
                    Console.WriteLine(
                        $"  {ErrorCount} Error(s), {WarningCount} Warning(s), {SuggestionCount} Suggestion(s)");
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
                output.WriteLine($"./{error.FilePath}({error.Line},{error.Column}): {error.Message}");
            }
        }

        public void Dispose()
        {
            lock (_outputLock)
            {
                if (_output != null && _output.IsValueCreated)
                {
                    _output.Value.Dispose();
                }
            }
        }

        private void WriteCore(Error error, ErrorLevel level)
        {
            Telemetry.TrackErrorCount(error.Code, level);

            if (_output != null)
            {
                var line = _legacy ? LegacyReport(error, level) : error.ToString(level);
                lock (_outputLock)
                {
                    _output.Value.WriteLine(line);
                }
            }

            PrintError(error, level);
        }

        private int GetMaxCount(Config? config, ErrorLevel level)
        {
            if (config == null)
            {
                return int.MaxValue;
            }

            switch (level)
            {
                case ErrorLevel.Error:
                    return config.MaxErrors;
                case ErrorLevel.Warning:
                    return config.MaxWarnings;
                case ErrorLevel.Suggestion:
                    return config.MaxSuggestions;
                default:
                    return int.MaxValue;
            }
        }

        private bool ExceedMaxErrors(Config? config, ErrorLevel level)
        {
            if (config == null)
            {
                return false;
            }

            switch (level)
            {
                case ErrorLevel.Error:
                    return Volatile.Read(ref _errorCount) >= config.MaxErrors;
                case ErrorLevel.Warning:
                    return Volatile.Read(ref _warningCount) >= config.MaxWarnings;
                case ErrorLevel.Suggestion:
                    return Volatile.Read(ref _suggestionCount) >= config.MaxSuggestions;
                default:
                    return false;
            }
        }

        private bool IncrementExceedMaxErrors(Config? config, ErrorLevel level)
        {
            if (config == null)
            {
                return false;
            }

            switch (level)
            {
                case ErrorLevel.Error:
                    return Interlocked.Increment(ref _errorCount) > config.MaxErrors;
                case ErrorLevel.Warning:
                    return Interlocked.Increment(ref _warningCount) > config.MaxWarnings;
                case ErrorLevel.Suggestion:
                    return Interlocked.Increment(ref _suggestionCount) > config.MaxSuggestions;
                default:
                    return false;
            }
        }

        private static ConsoleColor GetColor(ErrorLevel level)
        {
            switch (level)
            {
                case ErrorLevel.Error:
                    return ConsoleColor.Red;
                case ErrorLevel.Warning:
                    return ConsoleColor.Yellow;
                case ErrorLevel.Suggestion:
                    return ConsoleColor.Magenta;
                default:
                    return ConsoleColor.Cyan;
            }
        }

        private static string LegacyReport(Error error, ErrorLevel level)
        {
            var message_severity = level;
            var code = error.Code;
            var message = error.Message;
            var file = error.FilePath?.Path;
            var line = error.Line;
            var end_line = error.EndLine;
            var column = error.Column;
            var end_column = error.EndColumn;
            var date_time = DateTime.UtcNow;
            var log_item_type = "user";

            return JsonUtility.Serialize(new { message_severity, log_item_type, code, message, file, line, end_line, column, end_column, date_time });
        }
    }
}
