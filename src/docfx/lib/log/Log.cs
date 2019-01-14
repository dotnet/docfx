// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal static class Log
    {
        private static AsyncLocal<bool> t_verbose = new AsyncLocal<bool>();

        public static IDisposable BeginScope(bool verbose)
        {
            t_verbose.Value = verbose;
            return new LogScope(() => t_verbose.Value = false);
        }

        public static void Write(Exception exception)
        {
            Write(exception.ToString(), ConsoleColor.DarkRed);
        }

        public static void Write(string message, ConsoleColor color = ConsoleColor.DarkGray)
        {
            if (t_verbose.Value)
            {
#pragma warning disable CA2002
                lock (Console.Out)
#pragma warning restore CA2002
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
