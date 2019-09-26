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
        private readonly Func<Config> _config;

        private readonly ConcurrentHashSet<Error> _errors = new ConcurrentHashSet<Error>(Error.Comparer);
        private readonly Lazy<TextWriter> _output;

        private int _errorCount;
        private int _warningCount;
        private int _suggestionCount;

        private int _maxExceeded;

        public int ErrorCount => _errorCount;

        public int WarningCount => _warningCount;

        public int SuggestionCount => _suggestionCount;

        public ErrorLog(string docsetPath, string outputPath, Func<Config> config, bool legacy = false)
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

                    outputPath = Path.Combine(docsetPath, conf.Output.Path);
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

        public bool Write(Document file, IEnumerable<Error> errors)
        {
            var hasErrors = false;
            foreach (var error in errors)
            {
                if (Write(file, error))
                {
                    hasErrors = true;
                }
            }
            return hasErrors;
        }

        public bool Write(Document file, Error error, bool isException = false)
        {
            return Write(
                file.FilePath == error.FilePath || error.FilePath != null
                    ? error
                    : new Error(error.Level, error.Code, error.Message, file.FilePath, error.Line, error.Column),
                isException);
        }

        public bool Write(Error error, bool isException = false)
        {
            var config = _config();

            if (config != null && config.CustomErrors.TryGetValue(error.Code, out var customError))
            {
                error = error.WithCustomError(customError);
            }

            var level = isException ? ErrorLevel.Error : error.Level;
            if (level == ErrorLevel.Off)
            {
                return false;
            }

            if (config != null && config.WarningsAsErrors && level == ErrorLevel.Warning)
            {
                level = ErrorLevel.Error;
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

            Log.Error(error, level);
        }

        private int GetMaxCount(Config config, ErrorLevel level)
        {
            if (config == null)
            {
                return int.MaxValue;
            }

            switch (level)
            {
                case ErrorLevel.Error:
                    return config.Output.MaxErrors;
                case ErrorLevel.Warning:
                    return config.Output.MaxWarnings;
                case ErrorLevel.Suggestion:
                    return config.Output.MaxSuggestions;
                default:
                    return int.MaxValue;
            }
        }

        private bool ExceedMaxErrors(Config config, ErrorLevel level)
        {
            if (config == null)
            {
                return false;
            }

            switch (level)
            {
                case ErrorLevel.Error:
                    return Volatile.Read(ref _errorCount) >= config.Output.MaxErrors;
                case ErrorLevel.Warning:
                    return Volatile.Read(ref _warningCount) >= config.Output.MaxWarnings;
                case ErrorLevel.Suggestion:
                    return Volatile.Read(ref _suggestionCount) >= config.Output.MaxSuggestions;
                default:
                    return false;
            }
        }

        private bool IncrementExceedMaxErrors(Config config, ErrorLevel level)
        {
            if (config == null)
            {
                return false;
            }

            switch (level)
            {
                case ErrorLevel.Error:
                    return Interlocked.Increment(ref _errorCount) > config.Output.MaxErrors;
                case ErrorLevel.Warning:
                    return Interlocked.Increment(ref _warningCount) > config.Output.MaxWarnings;
                case ErrorLevel.Suggestion:
                    return Interlocked.Increment(ref _suggestionCount) > config.Output.MaxSuggestions;
                default:
                    return false;
            }
        }

        private static string LegacyReport(Error error, ErrorLevel level)
        {
            var message_severity = level;
            var code = error.Code;
            var message = error.Message;
            var file = error.FilePath?.Path;
            var line = error.Line;
            var date_time = DateTime.UtcNow;
            var origin = error.FilePath?.Origin != null && error.FilePath.Origin != default ? error.FilePath.Origin : (FileOrigin?)null;
            var log_item_type = "user";

            return JsonUtility.Serialize(new { message_severity, log_item_type, code, message, file, line, date_time, origin });
        }
    }
}
