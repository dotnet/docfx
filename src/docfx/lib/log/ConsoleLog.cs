// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal static class ConsoleLog
    {
        private static readonly object s_consoleLock = new object();
        private static string s_lastProgressName;
        private static DateTime s_lastProgressTime;
        private static AsyncLocal<ImmutableStack<LogScope>> t_scope = new AsyncLocal<ImmutableStack<LogScope>>();

        public static MeasureScope Measure(string name)
        {
            WriteProgress(name, name);

            var scope = new LogScope { Name = name, StartTime = DateTime.UtcNow };
            t_scope.Value = (t_scope.Value ?? ImmutableStack<LogScope>.Empty).Push(scope);
            return default;
        }

        public struct MeasureScope : IDisposable
        {
            public void Dispose()
            {
                t_scope.Value = t_scope.Value.Pop(out var scope);

                if (!scope.HasProgress)
                {
                    WriteProgress(scope.Name, $"{scope.Name} [{ElapsedTime(scope.StartTime)}]");
                }
            }
        }

        public static string ElapsedTime(DateTime startTime)
        {
            var elapsed = DateTime.UtcNow - startTime;
            return new TimeSpan(elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds).ToString();
        }

        public static void Progress(int done, int total)
        {
            try
            {
                Debug.Assert(t_scope.Value != null);

                var now = DateTime.UtcNow;
                if (done != total && now - s_lastProgressTime < TimeSpan.FromSeconds(1))
                {
                    return;
                }
                s_lastProgressTime = now;

                var scope = t_scope.Value.Peek();
                var percent = Math.Min(1.0, done / Math.Max(1.0, total));
                var head = $"{scope.Name} ";
                var tail = $" {(int)(percent * 100)}%, {done}/{total} [{ElapsedTime(scope.StartTime)}]";

                var width = Math.Min(40, Console.BufferWidth - 1 - head.Length - tail.Length - 2);
                var n = (int)(percent * width);
                var progress = width > 0 ? $"[{new string('#', n)}{new string(' ', Math.Max(0, width - n))}]" : "";
                var line = head + progress + tail;

                WriteProgress(scope.Name, line);

                scope.HasProgress = true;
            }
            catch
            {
                // Swallow console exceptions
            }
        }

        public static void WriteError(ErrorLevel level, Error error)
        {
            try
            {
                lock (s_consoleLock)
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
            catch
            {
                // Swallow console exceptions
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

        private static void WriteProgress(string name, string line)
        {
            lock (s_consoleLock)
            {
                if (s_lastProgressName == name)
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                }
                s_lastProgressName = name;
                Console.WriteLine(line.PadRight(Console.BufferWidth - 1 - line.Length, ' '));
            }
        }

        private class LogScope
        {
            public string Name;
            public DateTime StartTime;
            public bool HasProgress;
        }
    }
}
