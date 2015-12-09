// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    /// <summary>
    /// Replay log on flushing.
    /// </summary>
    public class ReplayLogListener : ILoggerListener
    {
        private readonly LogLevel _replayLevel;
        private readonly SortedList<LogLevel, List<ILogItem>> _replayList;
        private ImmutableArray<ILoggerListener> _listeners =
            ImmutableArray<ILoggerListener>.Empty;

        public ReplayLogListener(LogLevel replayLevel = LogLevel.Warning)
        {
            _replayLevel = replayLevel;
            _replayList = new SortedList<LogLevel, List<ILogItem>>();
            for (LogLevel level = replayLevel; level <= LogLevel.Error; level++)
            {
                _replayList.Add(level, new List<ILogItem>());
            }
        }

        public LogLevel LogLevelThreshold { get; set; }

        public void Dispose()
        {
            foreach (var listener in _listeners)
            {
                listener.Dispose();
            }
        }

        public void Flush()
        {
            WriteHeader();
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

            WriteFooter();

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

        private void WriteHeader()
        {
            var logLevel = _replayList.FirstOrDefault(s => s.Value.Count > 0).Key;
            string message;
            if (logLevel >= LogLevel.Error)
            {
                message = "Build failed.";
            }
            else if (logLevel == LogLevel.Warning)
            {
                message = "Build succeeded with warning.";
            }
            else
            {
                message = "Build succeeded.";
            }
            var logItem = new SimpleLogItem(logLevel, $"\n\n{message}", null);
            foreach (var listener in _listeners)
            {
                listener.WriteLine(logItem);
            }
        }

        private void WriteFooter()
        {
            var status = string.Join(", ", _replayList.Select(s => $"{s.Value.Count} {s.Key}(s)"));
            var logLevel = _replayList.FirstOrDefault(s => s.Value.Count > 0).Key;
            var header = $"\n\nThere are totally {status}";
            var logItem = new SimpleLogItem(logLevel, header, "Build Completed.");
            foreach (var listener in _listeners)
            {
                listener.WriteLine(logItem);
            }
        }

        private void WriteLineCore(ILogItem item)
        {
            foreach (var listener in _listeners)
            {
                listener.WriteLine(item);
            }
        }

        public void AddListener(ILoggerListener listener)
        {
            _listeners = _listeners.Add(listener);
        }

        private sealed class SimpleLogItem : ILogItem
        {
            public string File => null;

            public string Line => null;

            public LogLevel LogLevel { get; }

            public string Message { get; }

            public string Phase { get; }

            public SimpleLogItem(LogLevel logLevel, string message, string phase)
            {
                LogLevel = logLevel;
                Message = message;
                Phase = phase;
            }
        }
    }
}
