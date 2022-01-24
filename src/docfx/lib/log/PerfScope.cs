// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build;

internal struct PerfScope : IDisposable
{
    private string _message;
    private long _start;

    public static PerfScope Start(string message)
    {
        Log.Write(message + "...\r");
        return new PerfScope { _message = message, _start = Stopwatch.GetTimestamp() };
    }

    public void Dispose()
    {
        var elapsed = TimeSpan.FromSeconds(1.0 * (Stopwatch.GetTimestamp() - _start) / Stopwatch.Frequency);

        Log.Write($"{_message} done in {Progress.FormatTimeSpan(elapsed)}");
    }
}
