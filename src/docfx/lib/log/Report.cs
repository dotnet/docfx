// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal sealed class Report : IDisposable
    {
        public static readonly int MaxErrors = int.TryParse(Environment.GetEnvironmentVariable("DOCFX_MAX_ERRORS"), out var n) ? n : 1000;

        private readonly bool _legacy;
        private readonly object _outputLock = new object();
        private readonly ConcurrentHashSet<Error> _errors = new ConcurrentHashSet<Error>(Error.Comparer);

        private Lazy<TextWriter> _output;
        private Config _config;

        private int _errorCount;
        private int _warningCount;
        private int _infoCount;

        public int Errors => _errorCount;

        public int Warnings => _warningCount;

        public Report(bool legacy = false)
        {
            _legacy = legacy;
        }

        public void Configure(string docsetPath, Config config)
        {
            Debug.Assert(_output == null, "Cannot change report output path");

            _config = config;
            _output = new Lazy<TextWriter>(() =>
            {
                var outputFilePath = Path.GetFullPath(Path.Combine(docsetPath, config.Output.Path, "build.log"));

                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                return File.CreateText(outputFilePath);
            });
        }

        public bool Write(Error error)
        {
            var level = _config != null && _config.Rules.TryGetValue(error.Code, out var overrideLevel) ? overrideLevel : error.Level;
            if (level == ErrorLevel.Off)
            {
                return false;
            }

            if (_config != null && _config.WarningsAsErrors && level == ErrorLevel.Warning)
            {
                level = ErrorLevel.Error;
            }

            if (!ReachedMaxErrors() && _errors.TryAdd(error) && !IncrementReachedMaxErrors())
            {
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

            return level == ErrorLevel.Error;

            bool ReachedMaxErrors()
            {
                switch (level)
                {
                    case ErrorLevel.Error:
                        return _errorCount >= MaxErrors;
                    case ErrorLevel.Warning:
                        return _warningCount >= MaxErrors;
                    default:
                        return _infoCount >= MaxErrors;
                }
            }

            bool IncrementReachedMaxErrors()
            {
                switch (level)
                {
                    case ErrorLevel.Error:
                        return Interlocked.Increment(ref _errorCount) >= MaxErrors;
                    case ErrorLevel.Warning:
                        return Interlocked.Increment(ref _warningCount) >= MaxErrors;
                    default:
                        return Interlocked.Increment(ref _infoCount) >= MaxErrors;
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
                default:
                    return ConsoleColor.Cyan;
            }
        }
    }
}
