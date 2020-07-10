// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal sealed class ErrorLog : IDisposable
    {
        private readonly object _outputLock = new object();

        private readonly ConcurrentHashSet<Error> _errors = new ConcurrentHashSet<Error>(Error.Comparer);
        private readonly ConcurrentHashSet<FilePath> _errorFiles = new ConcurrentHashSet<FilePath>();

        private Lazy<TextWriter> _output;
        private Config? _config;
        private SourceMap? _sourceMap;

        private int _errorCount;
        private int _warningCount;
        private int _suggestionCount;

        private ConcurrentDictionary<FilePath, int> _fileInfoCount = new ConcurrentDictionary<FilePath, int>();
        private ConcurrentDictionary<FilePath, int> _fileSuggestionCount = new ConcurrentDictionary<FilePath, int>();
        private ConcurrentDictionary<FilePath, int> _fileWarningCount = new ConcurrentDictionary<FilePath, int>();
        private ConcurrentDictionary<FilePath, int> _fileErrorCount = new ConcurrentDictionary<FilePath, int>();

        private ConcurrentHashSet<FilePath> _fileInfoMaxExceeded = new ConcurrentHashSet<FilePath>();
        private ConcurrentHashSet<FilePath> _fileSuggestionMaxExceeded = new ConcurrentHashSet<FilePath>();
        private ConcurrentHashSet<FilePath> _fileWarningMaxExceeded = new ConcurrentHashSet<FilePath>();
        private ConcurrentHashSet<FilePath> _fileErrorMaxExceeded = new ConcurrentHashSet<FilePath>();

        public int ErrorCount => _errorCount;

        public int WarningCount => _warningCount;

        public int SuggestionCount => _suggestionCount;

        public bool HasError(FilePath file) => _errorFiles.Contains(file);

        public ErrorLog(string? outputPath = null)
        {
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
            if (config != null && config.CustomRules.TryGetValue(error.Code, out var customRule))
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

            if (config != null && error.FilePath != null && error.FilePath.Origin == FileOrigin.Fallback)
            {
                if (level == ErrorLevel.Error)
                {
                    return Write(Errors.Logging.FallbackError(config.DefaultLocale));
                }
                return false;
            }

            if (ExceedMaxErrors(config, level, error.FilePath))
            {
                WriteExceedMaxError(config, level, error.FilePath);
            }
            else if (_errors.TryAdd(error) && !IncrementExceedMaxErrors(config, level, error.FilePath))
            {
                IncrementActualErrorCount(level);
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

        private int GetFileMaxCount(Config? config, ErrorLevel level)
        {
            if (config == null)
            {
                return int.MaxValue;
            }

            return level switch
            {
                ErrorLevel.Error => config.MaxFileErrors,
                ErrorLevel.Warning => config.MaxFileWarnings,
                ErrorLevel.Suggestion => config.MaxFileSuggestions,
                ErrorLevel.Info => config.MaxFileInfos,
                _ => int.MaxValue,
            };
        }

        private bool ExceedMaxErrors(Config? config, ErrorLevel level, FilePath? filePath)
        {
            if (filePath == null)
            {
                return false;
            }

            if (TryGetFileCount(level, out var fileCount))
            {
                return fileCount.GetValueOrDefault(filePath, 0) >= GetFileMaxCount(config, level);
            }

            return false;
        }

        private bool IncrementExceedMaxErrors(Config? config, ErrorLevel level, FilePath? filePath)
        {
            if (filePath == null)
            {
                return false;
            }

            if (TryGetFileCount(level, out var fileCount))
            {
                return fileCount.AddOrUpdate(filePath, 1, (_, oldCount) => ++oldCount) > GetFileMaxCount(config, level);
            }

            return false;
        }

        private void WriteExceedMaxError(Config? config, ErrorLevel level, FilePath? filePath)
        {
            if (TryGetFileMaxExceeded(level, filePath))
            {
                WriteCore(Errors.Logging.ExceedFileMaxErrors(GetFileMaxCount(config, level), level, filePath), level);
            }
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

        private bool TryGetFileCount(ErrorLevel level, [NotNullWhen(returnValue: true)] out ConcurrentDictionary<FilePath, int>? fileCountDictionary)
        {
            fileCountDictionary = level switch
            {
                ErrorLevel.Error => _fileErrorCount,
                ErrorLevel.Warning => _fileWarningCount,
                ErrorLevel.Suggestion => _fileSuggestionCount,
                ErrorLevel.Info => _fileInfoCount,
                _ => null,
            };

            return fileCountDictionary != null;
        }

        private bool TryGetFileMaxExceeded(ErrorLevel level, [NotNullWhen(returnValue: true)] FilePath? filePath)
        {
            if (filePath == null)
            {
                return false;
            }

            return level switch
            {
                ErrorLevel.Error => _fileErrorMaxExceeded.TryAdd(filePath),
                ErrorLevel.Warning => _fileWarningMaxExceeded.TryAdd(filePath),
                ErrorLevel.Suggestion => _fileSuggestionMaxExceeded.TryAdd(filePath),
                ErrorLevel.Info => _fileInfoMaxExceeded.TryAdd(filePath),
                _ => false,
            };
        }

        private void IncrementActualErrorCount(ErrorLevel level)
        {
            switch (level)
            {
                case ErrorLevel.Error:
                    Interlocked.Increment(ref _errorCount);
                    break;
                case ErrorLevel.Warning:
                    Interlocked.Increment(ref _warningCount);
                    break;
                case ErrorLevel.Suggestion:
                    Interlocked.Increment(ref _suggestionCount);
                    break;
                default:
                    break;
            }
        }
    }
}
