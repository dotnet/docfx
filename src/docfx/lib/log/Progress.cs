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
        private static DateTime s_lastUpdateTime;

        public static IDisposable Start(string name)
        {
            var scope = new LogScope { Name = name, StartTime = DateTime.UtcNow };
            t_scope.Value = (t_scope.Value ?? ImmutableStack<LogScope>.Empty).Push(scope);
            return scope;
        }

        public static void Update(int done, int total)
        {
            Debug.Assert(t_scope.Value != null);

            // Throttle writing progress to console once every second.
            var now = DateTime.UtcNow;
            if (done != total && now - s_lastUpdateTime < TimeSpan.FromSeconds(1))
            {
                return;
            }
            s_lastUpdateTime = now;

            // Only write progress if it takes longer than 2 seconds
            var scope = t_scope.Value.Peek();
            if (now - scope.StartTime < TimeSpan.FromSeconds(2))
            {
                return;
            }

            var eol = done == total ? '\n' : '\r';
            var percent = ((int)(100 * Math.Min(1.0, done / Math.Max(1.0, total)))).ToString();

            Console.Write($"{scope.Name}: {percent.PadLeft(3)}% ({done}/{total}), {ElapsedTime(scope.StartTime)} {eol}");
        }

        internal static string ElapsedTime(DateTime startTime)
        {
            var elapsed = DateTime.UtcNow - startTime;
            return new TimeSpan(elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds).ToString();
        }

        private class LogScope : IDisposable
        {
            public string Name;
            public DateTime StartTime;

            public void Dispose()
            {
                t_scope.Value = t_scope.Value.Pop(out var scope);
            }
        }
    }
}
