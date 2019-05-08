// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal sealed class ErrorLog : IDisposable
    {
        private readonly bool _legacy;
        private readonly object _outputLock = new object();
        private readonly ConcurrentHashSet<Error> _errors = new ConcurrentHashSet<Error>(Error.Comparer);

        private readonly string _docsetPath;
        private Lazy<TextWriter> _output;
        private Config _config;

        private int _errorCount;
        private int _warningCount;
        private int _suggestionCount;
        private int _infoCount;

        private int _maxExceeded;

        public int ErrorCount => _errorCount;

        public int WarningCount => _warningCount;

        public int SuggestionCount => _suggestionCount;

        public ErrorLog(string docset = ".", bool legacy = false)
        {
            _docsetPath = docset;
            _legacy = legacy;
            _config = new Config();
            _output = new Lazy<TextWriter>(() =>
            {
                // add default build log file output path
                var outputFilePath = Path.GetFullPath(Path.Combine(_docsetPath, _config.Output.Path, ".errors.log"));

                PathUtility.CreateDirectoryFromFilePath(outputFilePath);

                return File.CreateText(outputFilePath);
            });
        }

        public void Configure(Config config)
        {
            Debug.Assert(!_output.IsValueCreated || _config.Output.Path == config.Output.Path, "Cannot change report output path");

            _config = config;
        }

        public bool Write(string file, IEnumerable<Error> errors)
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

        public bool Write(string file, Error error)
        {
            if (error is null)
                return false;

            return Write(file == error.File || !string.IsNullOrEmpty(error.File)
                    ? error
                    : new Error(error.Level, error.Code, error.Message, file, error.Line, error.Column));
        }

        public bool Write(Error error, bool force = false)
        {
            if (error is null)
                return false;

            var level = !force && _config != null && _config.Rules.TryGetValue(error.Code, out var overrideLevel)
                ? overrideLevel
                : error.Level;

            if (level == ErrorLevel.Off)
            {
                return false;
            }

            if (_config != null && _config.WarningsAsErrors && level == ErrorLevel.Warning)
            {
                level = ErrorLevel.Error;
            }

            var maxErrors = _config?.Output.MaxErrors ?? OutputConfig.DefaultMaxErrors;
            if (ReachMaxErrors())
            {
                if (Interlocked.Exchange(ref _maxExceeded, 1) == 0)
                {
                    WriteCore(Errors.ExceedMaxErrors(maxErrors, level), level);
                }
            }
            else if (_errors.TryAdd(error) && !IncrementExceedMaxErrors())
            {
                WriteCore(error, level);
            }

            return level == ErrorLevel.Error;

            bool ReachMaxErrors()
            {
                switch (level)
                {
                    case ErrorLevel.Error:
                        return Volatile.Read(ref _errorCount) >= maxErrors;
                    case ErrorLevel.Warning:
                        return Volatile.Read(ref _warningCount) >= maxErrors;
                    case ErrorLevel.Suggestion:
                        return Volatile.Read(ref _suggestionCount) >= maxErrors;
                    default:
                        return Volatile.Read(ref _infoCount) >= maxErrors;
                }
            }

            bool IncrementExceedMaxErrors()
            {
                switch (level)
                {
                    case ErrorLevel.Error:
                        return Interlocked.Increment(ref _errorCount) > maxErrors;
                    case ErrorLevel.Warning:
                        return Interlocked.Increment(ref _warningCount) > maxErrors;
                    case ErrorLevel.Suggestion:
                        return Interlocked.Increment(ref _suggestionCount) > maxErrors;
                    default:
                        return Interlocked.Increment(ref _infoCount) > maxErrors;
                }
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

            ConsoleLog(level, error);
        }

        private static string LegacyReport(Error error, ErrorLevel level)
        {
            var message_severity = level;
            var code = error.Code;
            var message = error.Message;
            var file = error.File;
            var line = error.Line;
            var date_time = DateTime.UtcNow;

            return JsonUtility.Serialize(new { message_severity, code, message, file, line, date_time });
        }

        private static void ConsoleLog(ErrorLevel level, Error error)
        {
            // https://github.com/dotnet/corefx/issues/2808
            // Do not lock on objects with weak identity,
            // but since this is the only way to synchronize console color
#pragma warning disable CA2002
            lock (Console.Out)
#pragma warning restore CA2002
            {
                var output = level == ErrorLevel.Error ? Console.Error : Console.Out;
                Console.ForegroundColor = GetColor(level);
                output.Write(error.Code + " ");
                Console.ResetColor();
                output.WriteLine($"{error.File}({error.Line},{error.Column}): {error.Message}");
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
    }
}
