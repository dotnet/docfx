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
            if (elapsedMs < 2000)
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

        private class LogScope : IDisposable
        {
            public string Name;
            public long LastElapsedMs;
            public Stopwatch Stopwatch;

            public void Dispose()
            {
                t_scope.Value = t_scope.Value.Pop(out var scope);
                Console.WriteLine($"{scope.Name} done in {TimeSpan.FromSeconds(Stopwatch.ElapsedMilliseconds / 1000)}");
            }
        }
    }
}
