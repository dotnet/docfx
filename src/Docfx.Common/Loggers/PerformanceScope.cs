// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Docfx.Common;

public sealed class PerformanceScope : IDisposable
{
    private readonly Stopwatch _stopwatch = new();
    private readonly AggregatedPerformanceScope _aggregatedPerformanceLogger = null;
    private readonly Action<TimeSpan> _logger;

    public PerformanceScope(string content, LogLevel level) : this(s => Logger.Log(level, GetContent(content, s)))
    {
    }

    public PerformanceScope(string content) : this(content, LogLevel.Verbose)
    {
    }

    public PerformanceScope(string content, LogLevel level, AggregatedPerformanceScope aggregatedPerformanceLogger) : this(content, level)
    {
        _aggregatedPerformanceLogger = aggregatedPerformanceLogger;
    }

    public PerformanceScope(Action<TimeSpan> logger = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stopwatch.Restart();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _logger(_stopwatch.Elapsed);
        _aggregatedPerformanceLogger?.Log(_stopwatch.Elapsed);
    }

    private static string GetContent(string content, TimeSpan span)
    {
        if (string.IsNullOrEmpty(content))
        {
            return $"Completed in {span.TotalMilliseconds} milliseconds";
        }

        return $"Completed {content} in {span.TotalMilliseconds} milliseconds.";
    }
}
