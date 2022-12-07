// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    /// <summary>
    /// Replay log on flushing.
    /// </summary>
    [Obsolete]
    public class ReplayLogListener : ILoggerListener
    {
        private readonly LogLevel _replayLevel;
        private readonly SortedList<LogLevel, List<ILogItem>> _replayList;
        private ImmutableArray<ILoggerListener> _listeners =
            ImmutableArray<ILoggerListener>.Empty;

        public bool Replay { get; set; } = true;

        public ReplayLogListener(LogLevel replayLevel = LogLevel.Warning)
        {
            _replayLevel = replayLevel;
            _replayList = new SortedList<LogLevel, List<ILogItem>>();
            for (LogLevel level = replayLevel; level <= LogLevel.Error; level++)
            {
                _replayList.Add(level, new List<ILogItem>());
            }
        }

        public void Dispose()
        {
            foreach (var listener in _listeners)
            {
                listener.Dispose();
            }
        }

        public void AddListener(ILoggerListener listener)
        {
            _listeners = _listeners.Add(listener);
        }

        public void Flush()
        {
            if (!Replay) return;

            var logLevel = _replayList.LastOrDefault(s => s.Value.Count > 0).Key;
            var buildStatus = GetBuildStatusFromLogLevel(logLevel);
            WriteHeader(buildStatus);
            foreach (var list in _replayList)
            {
                foreach (var item in list.Value)
                {
                    WriteLineCore(item);
                }
            }

            foreach (var listener in _listeners)
            {
                listener.Flush();
            }

            WriteFooter(buildStatus);

            foreach (var level in _replayList.Keys.ToList())
            {
                _replayList[level] = new List<ILogItem>();
            }
        }

        public void WriteLine(ILogItem item)
        {
            if (item.LogLevel >= _replayLevel && item.LogLevel <= LogLevel.Error)
            {
                _replayList[item.LogLevel].Add(item);
            }
            WriteLineCore(item);
        }

        private void WriteHeader(BuildStatus status)
        {
            string message = Environment.NewLine + Environment.NewLine;
            switch (status)
            {
                case BuildStatus.Failed:
                    message += "Build failed.";
                    break;
                case BuildStatus.SucceedWithWarning:
                    message += "Build succeeded with warning.";
                    break;
                case BuildStatus.Succeed:
                    message += "Build succeeded.";
                    break;
                default:
                    break;
            }

            WriteToConsole(message, status);
        }

        private void WriteFooter(BuildStatus status)
        {
            var footer = string.Join(Environment.NewLine, _replayList.Select(s => $"\t{s.Value.Count} {s.Key}(s)"));
            WriteToConsole(footer, status);
        }

        private void WriteLineCore(ILogItem item)
        {
            foreach (var listener in _listeners)
            {
                listener.WriteLine(item);
            }
        }

        private static void WriteToConsole(string message, BuildStatus status)
        {
            switch (status)
            {
                case BuildStatus.Failed:
                    WriteToConsole(message, ConsoleColor.Red);
                    break;
                case BuildStatus.SucceedWithWarning:
                    WriteToConsole(message, ConsoleColor.Yellow);
                    break;
                case BuildStatus.Succeed:
                    WriteToConsole(message, ConsoleColor.Green);
                    break;
            }
        }

        private static void WriteToConsole(string message, ConsoleColor color = ConsoleColor.White)
        {
            ConsoleUtility.WriteLine(message, color);
        }

        private static BuildStatus GetBuildStatusFromLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error:
                    return BuildStatus.Failed;
                case LogLevel.Warning:
                    return BuildStatus.SucceedWithWarning;
                default:
                    return BuildStatus.Succeed;
            }
        }

        private enum BuildStatus
        {
            Failed,
            SucceedWithWarning,
            Succeed
        }
    }
}
