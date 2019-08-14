// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;

    public sealed class AggregatedPerformanceScope : IDisposable
    {
        private readonly ConcurrentDictionary<string, Lazy<AggregatedPerformance>> _aggregatedPerformanceByPhase = new ConcurrentDictionary<string, Lazy<AggregatedPerformance>>();
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
}
