// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#if NetCore
    using AggregatedList = System.Collections.Generic.SortedDictionary<LogLevel, System.Collections.Generic.List<ILogItem>>;
#else
    using AggregatedList = System.Collections.Generic.SortedList<LogLevel, System.Collections.Generic.List<ILogItem>>;
#endif
    /// <summary>
    /// Replay aggregated log on flushing
    /// </summary>
    public class AggregatedLogListener : ILoggerListener
    {
        private readonly LogLevel _threshold;
        private readonly ILoggerListener _innerListener;
        private readonly AggregatedList _aggregatedList;

        public AggregatedLogListener(AggregatedLogListener other) : this(other._threshold)
        {
            _aggregatedList = other._aggregatedList;
        }

        public AggregatedLogListener(LogLevel threshold = LogLevel.Warning)
        {
            _threshold = threshold;
            _aggregatedList = new AggregatedList();
            _innerListener = new ConsoleLogListener();
            for (LogLevel level = _threshold; level <= LogLevel.Error; level++)
            {
                _aggregatedList.Add(level, new List<ILogItem>());
            }
        }

        public void Flush()
        {
            var logLevel = _aggregatedList.LastOrDefault(s => s.Value.Count > 0).Key;
            var buildStatus = GetBuildStatusFromLogLevel(logLevel);
            WriteHeader(buildStatus);
            foreach (var list in _aggregatedList)
            {
                foreach (var item in list.Value)
                {
                    WriteLineCore(item);
                }
            }

            _innerListener.Flush();
            WriteFooter(buildStatus);
            foreach (var level in _aggregatedList.Keys.ToList())
            {
                _aggregatedList[level] = new List<ILogItem>();
            }
        }

        public void Dispose()
        {
            _innerListener.Dispose();
        }

        public void WriteLine(ILogItem item)
        {
            if (item.LogLevel >= _threshold && item.LogLevel <= LogLevel.Error)
            {
                _aggregatedList[item.LogLevel].Add(item);
            }
        }

        private void WriteHeader(BuildStatus status)
        {
            string message = Environment.NewLine + Environment.NewLine;
            switch (status)
            {
                case BuildStatus.Failed:
                    message += "Build failed.";
                    break;
                case BuildStatus.SucceededWithWarning:
                    message += "Build succeeded with warning.";
                    break;
                case BuildStatus.Succeeded:
                    message += "Build succeeded.";
                    break;
                default:
                    break;
            }
#if !NetCore
            WriteToConsole(message, status);
#endif
        }

        private void WriteFooter(BuildStatus status)
        {
            var footer = string.Join(Environment.NewLine, _aggregatedList.Select(s => $"\t{s.Value.Count} {s.Key}(s)"));
#if !NetCore
            WriteToConsole(footer, status);
#endif
        }

        private void WriteLineCore(ILogItem item)
        {
            _innerListener.WriteLine(item);
        }

#if !NetCore
        private static void WriteToConsole(string message, BuildStatus status)
        {
            switch (status)
            {
                case BuildStatus.Failed:
                    WriteToConsole(message, ConsoleColor.Red);
                    break;
                case BuildStatus.SucceededWithWarning:
                    WriteToConsole(message, ConsoleColor.Yellow);
                    break;
                case BuildStatus.Succeeded:
                    WriteToConsole(message, ConsoleColor.Green);
                    break;
                default:
                    break;
            }
        }

        private static void WriteToConsole(string message, ConsoleColor color = ConsoleColor.White)
        {
            ConsoleUtility.WriteLine(message, color);
        }
#endif

        private static BuildStatus GetBuildStatusFromLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error:
                    return BuildStatus.Failed;
                case LogLevel.Warning:
                    return BuildStatus.SucceededWithWarning;
                default:
                    return BuildStatus.Succeeded;
            }
        }

        private enum BuildStatus
        {
            Failed,
            SucceededWithWarning,
            Succeeded
        }
    }
}
