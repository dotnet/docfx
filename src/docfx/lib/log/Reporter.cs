// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal sealed class Reporter : IDisposable
    {
        private readonly object _lock = new object();
        private Lazy<TextWriter> _output;

        public void Configure(string docsetPath, Config config)
        {
            Debug.Assert(_output == null, "Cannot change report output path");

            // TODO: errors and warnings before config loaded are lost, need a way to report them back to host
            _output = new Lazy<TextWriter>(() =>
            {
                var outputFilePath = Path.GetFullPath(Path.Combine(docsetPath, config.Output.Path, "build.log"));

                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                return File.CreateText(outputFilePath);
            });
        }

        public void Report(Error error)
        {
            if (error.Level == ErrorLevel.Off)
            {
                return;
            }

            var outputMessage = error.ToString();

            lock (_lock)
            {
                if (_output != null)
                {
                    _output.Value.WriteLine(outputMessage);
                }

                if (!string.IsNullOrEmpty(error.File))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write(error.File);
                    Console.ResetColor();
                    Console.WriteLine();
                }

                Console.ForegroundColor = GetColor(error.Level);
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
            lock (_lock)
            {
                if (_output != null && _output.IsValueCreated)
                {
                    _output.Value.Dispose();
                }
            }
        }
    }
}
