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
        private static readonly AsyncLocal<bool> t_verbose = new();

        public static bool Verbose => TestQuirks.Verbose ?? t_verbose.Value;

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

        private class LogScope : IDisposable
        {
            private readonly Action _dispose;

            public LogScope(Action dispose) => _dispose = dispose;

            public void Dispose() => _dispose();
        }
    }
}
