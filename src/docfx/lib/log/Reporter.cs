// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

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

        public void Report(ReportLevel level, string code, string message, string file = "", int line = 0, int column = 0)
        {
            Debug.Assert(!string.IsNullOrEmpty(code));
            Debug.Assert(Regex.IsMatch(code, "^[a-z0-9-]{5,32}$"), "Error code should only contain dash and letters in lowercase");
            Debug.Assert(!string.IsNullOrEmpty(message));

            string outputMessage = null;

            if (_output != null)
            {
                var payload = new List<object> { level, code, message, file, line, column };
                for (var i = payload.Count - 1; i >= 0; i--)
                {
                    if (payload[i] == null || Equals(payload[i], "") || Equals(payload[i], 0))
                        payload.RemoveAt(i);
                    else
                        break;
                }
                outputMessage = JsonUtility.Serialize(payload);
            }

            lock (_lock)
            {
                if (_output != null)
                {
                    _output.Value.WriteLine(outputMessage);
                }

                if (!string.IsNullOrEmpty(file))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write(file);
                    Console.ResetColor();
                    Console.WriteLine();
                }

                Console.ForegroundColor = GetColor(level);
                Console.Write(code + " ");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        private static ConsoleColor GetColor(ReportLevel level)
        {
            switch (level)
            {
                case ReportLevel.Error:
                    return ConsoleColor.Red;
                case ReportLevel.Warning:
                    return ConsoleColor.Yellow;
                default:
                    return ConsoleColor.Green;
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
