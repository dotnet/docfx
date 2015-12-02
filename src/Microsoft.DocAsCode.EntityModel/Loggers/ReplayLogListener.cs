// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
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

        public ReplayLogListener(LogLevel replayLevel)
        {
            _replayLevel = replayLevel;
            _replayList = new SortedList<LogLevel, List<ILogItem>>();
            for (LogLevel level = replayLevel; level < LogLevel.Error; level++)
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
            foreach (var level in _replayList.Keys.ToList())
            {
                foreach (var item in _replayList[level])
                {
                    WriteLineCore(item);
                }
                _replayList[level] = new List<ILogItem>();
            }
            foreach (var listener in _listeners)
            {
                listener.Flush();
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
    }
}
