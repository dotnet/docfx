// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal sealed class Report : IDisposable
    {
        private readonly object _consoleLock = new object();
        private readonly object _outputLock = new object();
        private Lazy<TextWriter> _output;
        private Dictionary<string, ErrorLevel> _rules;

        public void Configure(string docsetPath, Config config)
        {
            Debug.Assert(_output == null, "Cannot change report output path");

            _rules = config.Rules;
            _output = new Lazy<TextWriter>(() =>
            {
                var outputFilePath = Path.GetFullPath(Path.Combine(docsetPath, config.Output.Path, "build.log"));

                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                return File.CreateText(outputFilePath);
            });
        }

        public void Write(Error error)
        {
            var level = _rules != null && _rules.TryGetValue(error.Code, out var overrideLevel) ? overrideLevel : error.Level;
            if (level == ErrorLevel.Off)
            {
                return;
            }

            if (_output != null)
            {
                lock (_outputLock)
                {
                    _output.Value.WriteLine(error.ToString(level));
                }
            }

            ConsoleLog(level, error);
        }

        private void ConsoleLog(ErrorLevel level, Error error)
        {
            lock (_consoleLock)
            {
                if (!string.IsNullOrEmpty(error.File))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write(error.File + ":");
                    Console.ResetColor();
                    Console.WriteLine();
                }

                Console.ForegroundColor = GetColor(level);
                Console.Write(error.Code + " ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(error.Message);
                Console.ResetColor();
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
    }
}
