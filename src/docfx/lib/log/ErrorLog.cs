// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal sealed class ErrorLog : IDisposable
    {
        private readonly bool _legacy;
        private readonly object _outputLock = new object();

        private readonly ConcurrentHashSet<Error> _errors = new ConcurrentHashSet<Error>(Error.Comparer);
        private readonly ConcurrentHashSet<FilePath> _errorFiles = new ConcurrentHashSet<FilePath>();

        private Lazy<TextWriter> _output;
        private Config? _config;
        private SourceMap? _sourceMap;

        private int _errorCount;
        private int _warningCount;
        private int _suggestionCount;

        private int _maxExceeded;

        public int ErrorCount => _errorCount;

        public int WarningCount => _warningCount;

        public int SuggestionCount => _suggestionCount;

        public IEnumerable<FilePath> ErrorFiles => _errorFiles;

        public ErrorLog(string? outputPath = null, bool legacy = false)
        {
            _legacy = legacy;
            _output = new Lazy<TextWriter>(() => outputPath is null ? TextWriter.Null : CreateOutput(outputPath));
        }

        public void Configure(Config config, string outputPath, SourceMap? sourceMap)
        {
            _config = config;
            _sourceMap = sourceMap;

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
                    return Write(Errors.Logging.FallbackError(config.DefaultLocale));
                }
                return false;
            }

            if (ExceedMaxErrors(config, level))
            {
                if (Interlocked.Exchange(ref _maxExceeded, 1) == 0)
                {
                    WriteCore(Errors.Logging.ExceedMaxErrors(GetMaxCount(config, level), level), level);
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
                output.WriteLine($"./{error.FilePath}({error.Line},{error.Column}): {error.Message}");
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

            if (level == ErrorLevel.Error && error.FilePath != null)
            {
                _errorFiles.TryAdd(error.FilePath);
            }

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

        private int GetMaxCount(Config? config, ErrorLevel level)
        {
            if (config == null)
            {
                return int.MaxValue;
            }

            return level switch
            {
                ErrorLevel.Error => config.MaxErrors,
                ErrorLevel.Warning => config.MaxWarnings,
                ErrorLevel.Suggestion => config.MaxSuggestions,
                _ => int.MaxValue,
            };
        }

        private bool ExceedMaxErrors(Config? config, ErrorLevel level)
        {
            if (config == null)
            {
                return false;
            }

            return level switch
            {
                ErrorLevel.Error => Volatile.Read(ref _errorCount) >= config.MaxErrors,
                ErrorLevel.Warning => Volatile.Read(ref _warningCount) >= config.MaxWarnings,
                ErrorLevel.Suggestion => Volatile.Read(ref _suggestionCount) >= config.MaxSuggestions,
                _ => false,
            };
        }

        private bool IncrementExceedMaxErrors(Config? config, ErrorLevel level)
        {
            return level switch
            {
                ErrorLevel.Error => Interlocked.Increment(ref _errorCount) > (config?.MaxErrors ?? int.MaxValue),
                ErrorLevel.Warning => Interlocked.Increment(ref _warningCount) > (config?.MaxWarnings ?? int.MaxValue),
                ErrorLevel.Suggestion => Interlocked.Increment(ref _suggestionCount) > (config?.MaxSuggestions ?? int.MaxValue),
                _ => false,
            };
        }

        private static ConsoleColor GetColor(ErrorLevel level)
        {
            return level switch
            {
                ErrorLevel.Error => ConsoleColor.Red,
                ErrorLevel.Warning => ConsoleColor.Yellow,
                ErrorLevel.Suggestion => ConsoleColor.Magenta,
                _ => ConsoleColor.Cyan,
            };
        }
    }
}
