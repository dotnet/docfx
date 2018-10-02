// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal static class Progress
    {
        private const int ProgressDelayMs = 2000;
        private static readonly AsyncLocal<ImmutableStack<LogScope>> t_scope = new AsyncLocal<ImmutableStack<LogScope>>();

        public static IDisposable Start(string name)
        {
            var scope = new LogScope { Name = name, Stopwatch = Stopwatch.StartNew() };
            t_scope.Value = (t_scope.Value ?? ImmutableStack<LogScope>.Empty).Push(scope);
            return scope;
        }

        public static void Update(int done, int total)
        {
            Debug.Assert(t_scope.Value != null);

            var scope = t_scope.Value.Peek();
            Debug.Assert(scope != null);

            // Only write progress if it takes longer than 2 seconds
            var elapsedMs = scope.Stopwatch.ElapsedMilliseconds;
            if (elapsedMs < ProgressDelayMs)
            {
                return;
            }

            // Throttle writing progress to console once every second.
            if (done != total && elapsedMs - scope.LastElapsedMs < 1000)
            {
                return;
            }
            scope.LastElapsedMs = elapsedMs;

            var eol = done == total ? '\n' : '\r';
            var percent = ((int)(100 * Math.Min(1.0, done / Math.Max(1.0, total)))).ToString();
            var duration = TimeSpan.FromSeconds(elapsedMs / 1000);

            Console.Write($"{scope.Name}: {percent.PadLeft(3)}% ({done}/{total}), {duration} {eol}");
        }

        public static string FormatTimeSpan(TimeSpan value)
        {
            if (value.TotalMinutes > 1)
                return TimeSpan.FromSeconds(value.TotalSeconds).ToString();
            if (value.TotalSeconds > 1)
                return Math.Round(value.TotalSeconds, digits: 2) + "s";
            return Math.Round(value.TotalMilliseconds, digits: 2) + "ms";
        }

        private class LogScope : IDisposable
        {
            public string Name;
            public long LastElapsedMs;
            public Stopwatch Stopwatch;

            public void Dispose()
            {
                t_scope.Value = t_scope.Value.Pop(out var scope);

                var elapsedMs = Stopwatch.ElapsedMilliseconds;
                if (elapsedMs > ProgressDelayMs)
                {
                    Console.WriteLine($"{Name} done in {FormatTimeSpan(Stopwatch.Elapsed)}");
                }
            }
        }
    }
}
