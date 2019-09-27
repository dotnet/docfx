// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.Docs.Build
{
    [SuppressMessage("Reliability", "CA2002", Justification = "Lock Console.Out")]
    internal static class Log
    {
        internal static bool ForceVerbose;

        private static AsyncLocal<bool> t_verbose = new AsyncLocal<bool>();

        public static bool Verbose => ForceVerbose || t_verbose.Value;

        public static IDisposable BeginScope(bool verbose)
        {
            t_verbose.Value = verbose;
            return new LogScope(() => t_verbose.Value = false);
        }

        public static void Important(string message, ConsoleColor color)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void Error(Error error, ErrorLevel? level = null)
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

        public static void Write(Exception exception)
        {
            Write(exception.ToString(), ConsoleColor.DarkRed);
        }

        public static void Write(string message, ConsoleColor color = ConsoleColor.DarkGray)
        {
            if (Verbose)
            {
                lock (Console.Out)
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(message);
                    Console.ResetColor();
                }
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

        private class LogScope : IDisposable
        {
            private readonly Action _dispose;

            public LogScope(Action dispose) => _dispose = dispose;

            public void Dispose() => _dispose();
        }
    }
}
