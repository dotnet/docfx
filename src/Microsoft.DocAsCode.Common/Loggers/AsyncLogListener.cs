// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class AsyncLogListener : ILoggerListener
    {
        private BlockingCollection<ILogItem> _logQueue = new BlockingCollection<ILogItem>();
        private readonly ManualResetEvent _signal = new ManualResetEvent(true);
        private readonly CompositeLogListener _inner;
        private readonly Lazy<Task> _loggingTask;
        private readonly int TimeoutMilliseconds = 300000; // 5 minutes

        public AsyncLogListener() : this(new CompositeLogListener())
        {
        }

        public AsyncLogListener(IEnumerable<ILoggerListener> listeners) : this(new CompositeLogListener(listeners))
        {
        }

        public AsyncLogListener(CompositeLogListener compositeLogListener)
        {
            _inner = compositeLogListener ?? throw new ArgumentNullException(nameof(compositeLogListener));
            _loggingTask = new Lazy<Task>(CreateLoggingTask);
        }

        public void AddListener(ILoggerListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            _inner.AddListener(listener);
        }

        public void AddListeners(IEnumerable<ILoggerListener> listeners)
        {
            if (listeners == null)
            {
                throw new ArgumentNullException(nameof(listeners));
            }

            _inner.AddListeners(listeners);
        }

        public ILoggerListener FindListener(Predicate<ILoggerListener> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return _inner.FindListener(predicate);
        }

        public void RemoveListener(ILoggerListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            WaitForLoggingComplete();
            _inner.RemoveListener(listener);
        }

        public void RemoveAllListeners()
        {
            WaitForLoggingComplete();
            _inner.RemoveAllListeners();
        }

        public void WriteLine(ILogItem item)
        {
            if (_inner.Count == 0)
            {
                return;
            }
            AddLogToQueue(item);
        }

        public void Flush()
        {
            WaitForLoggingComplete();
            _inner.Flush();
        }

        public void Dispose()
        {
            _logQueue.CompleteAdding();
            WaitForLoggingComplete();
            _inner.Dispose();
        }

        private void WaitForLoggingComplete()
        {
            _signal.WaitOne(TimeoutMilliseconds);
        }

        private void AddLogToQueue(ILogItem item)
        {
            InitTask();
            _signal.Reset();
            _logQueue.Add(item);
        }

        private Task InitTask()
        {
            return _loggingTask.Value;
        }

        private Task CreateLoggingTask()
        {
            return Task.Factory.StartNew(LoggingTask, TaskCreationOptions.LongRunning);
        }

        private void LoggingTask()
        {
            while (!_logQueue.IsCompleted)
            {
                if (_logQueue.Count == 0)
                {
                    _signal.Set();
                }

                ILogItem item = _logQueue.Take();
                try
                {
                    LogCore(item);
                }
                catch
                {
                    // Ignore logging error
                }
            }

            _signal.Set();
            _logQueue.Dispose();
        }

        private void LogCore(ILogItem item)
        {
            _inner.WriteLine(item);
        }
    }
}