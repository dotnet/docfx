// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NetCore
namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Diagnostics;

    public sealed class PerformanceScope : IDisposable
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Action<TimeSpan> _logger;

        public PerformanceScope(string content, LogLevel level) : this(s => Logger.Log(level, GetContent(content, s)))
        {
        }

        public PerformanceScope(string content) : this(content, LogLevel.Verbose)
        {
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
}
#endif