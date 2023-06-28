// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Microsoft.DocAsCode.Common;

public sealed class AggregatedPerformanceScope : IDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<AggregatedPerformance>> _aggregatedPerformanceByPhase = new();
    private readonly LogLevel _logLevel = LogLevel.Verbose;

    public AggregatedPerformanceScope(LogLevel? logLevel = null)
    {
        if (logLevel != null)
        {
            _logLevel = logLevel.Value;
        }
    }

    public void Log(TimeSpan elapsedTime)
    {
        var phaseName = LoggerPhaseScope.GetPhaseName();
        if (string.IsNullOrEmpty(phaseName))
        {
            return;
        }

        var aggregatedPerformanceByPhase = _aggregatedPerformanceByPhase.GetOrAdd(phaseName, _ => new Lazy<AggregatedPerformance>(() => new AggregatedPerformance())).Value;

        aggregatedPerformanceByPhase.Log(elapsedTime.TotalMilliseconds);
    }

    public void Dispose()
    {
        foreach (var aggregatedPerformanceByPhase in _aggregatedPerformanceByPhase.OrderBy(kvp => kvp.Key))
        {
            var aggregatedPerformance = aggregatedPerformanceByPhase.Value.Value;
            Logger.Log(_logLevel, $"Phase '{aggregatedPerformanceByPhase.Key}' runs {aggregatedPerformance.Occurrence} times with average time of {aggregatedPerformance.AverageTimeInMilliseconds} milliseconds.");
        }
    }

    private class AggregatedPerformance
    {
        private long _occurrence = 0;
        private double _totalTimeInMilliseconds = 0.0;

        public long Occurrence => _occurrence;

        public double TotalTimeInMilliseconds => _totalTimeInMilliseconds;

        public double AverageTimeInMilliseconds => _occurrence == 0 ? 0 : _totalTimeInMilliseconds / _occurrence;

        public void Log(double elapsedTimeInMilliSeconds)
        {
            if (elapsedTimeInMilliSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedTimeInMilliSeconds), "elapsed time in milliseconds must be greater than 0.");
            }

            Interlocked.Increment(ref _occurrence);

            while (true)
            {
                var currentTotalTime = _totalTimeInMilliseconds;
                var updatedTotalTime = currentTotalTime + elapsedTimeInMilliSeconds;
                if (Interlocked.CompareExchange(ref _totalTimeInMilliseconds, updatedTotalTime, currentTotalTime) == currentTotalTime)
                {
                    break;
                }
            }
        }
    }
}
