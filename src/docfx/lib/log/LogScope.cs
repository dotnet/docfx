// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build;

internal class LogScope : IDisposable
{
    public string Name { get; }

    public Stopwatch Stopwatch { get; }

    public long LastElapsedMs { get; set; }

    public LogScope(string name, Stopwatch stopwatch)
    {
        Name = name;
        Stopwatch = stopwatch;
    }

    public void Dispose()
    {
        var elapsedMs = Stopwatch.ElapsedMilliseconds;
        if (Log.Verbose || elapsedMs > Progress.ProgressDelayMs)
        {
            Console.WriteLine($"{Name} done in {Progress.FormatTimeSpan(Stopwatch.Elapsed)}");
        }
    }
}
