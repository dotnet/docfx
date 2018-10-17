// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
#if NetCore
    using ReplayList = System.Collections.Generic.SortedDictionary<LogLevel, System.Collections.Generic.List<ILogItem>>;
#else
    using ReplayList = System.Collections.Generic.SortedList<LogLevel, System.Collections.Generic.List<ILogItem>>;
#endif
    /// <summary>
    /// Replay log on flushing.
    /// </summary>
    [Obsolete]
    public class ReplayLogListener : ILoggerListener
    {
        private readonly LogLevel _replayLevel;
        private readonly ReplayList _replayList;
        private ImmutableArray<ILoggerListener> _listeners =
            ImmutableArray<ILoggerListener>.Empty;

        public bool Replay { get; set; } = true;

        public ReplayLogListener(LogLevel replayLevel = LogLevel.Warning)
        {
            _replayLevel = replayLevel;
            _replayList = new ReplayList();
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
#if !NetCore
            WriteToConsole(message, status);
#endif
        }

        private void WriteFooter(BuildStatus status)
        {
            var footer = string.Join(Environment.NewLine, _replayList.Select(s => $"\t{s.Value.Count} {s.Key}(s)"));
#if !NetCore
            WriteToConsole(footer, status);
#endif
        }

        private void WriteLineCore(ILogItem item)
        {
            foreach (var listener in _listeners)
            {
                listener.WriteLine(item);
            }
        }

#if !NetCore
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
#endif

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
