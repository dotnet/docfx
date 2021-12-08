// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build;

internal static class Progress
{
    public const int ProgressDelayMs = 2000;

    public static LogScope Start(string name)
    {
        var scope = new LogScope(name, Stopwatch.StartNew());
        if (Log.Verbose)
        {
            Console.Write(scope.Name + "...\r");
        }

        return scope;
    }

    public static void Update(LogScope scope, int done, int total)
    {
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

        Console.Write($"{scope.Name}: {percent,3}% ({done}/{total}), {duration} {eol}");
    }

    public static string FormatTimeSpan(TimeSpan value)
    {
        if (value.TotalMinutes > 1)
        {
            return TimeSpan.FromSeconds(value.TotalSeconds).ToString();
        }

        if (value.TotalSeconds > 1)
        {
            return Math.Round(value.TotalSeconds, digits: 2) + "s";
        }

        return Math.Round(value.TotalMilliseconds, digits: 2) + "ms";
    }
}
