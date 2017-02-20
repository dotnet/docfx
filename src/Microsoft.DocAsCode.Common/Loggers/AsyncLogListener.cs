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

        private readonly int TimeoutMilliseconds = 300000; // 5 minutes

        public AsyncLogListener() : this(new CompositeLogListener())
        {
        }

        public AsyncLogListener(IEnumerable<ILoggerListener> listeners) : this(new CompositeLogListener(listeners))
        {
        }

        public AsyncLogListener(CompositeLogListener compositeLogListener)
        {
            if (compositeLogListener == null)
            {
                throw new ArgumentNullException(nameof(compositeLogListener));
            }
            _inner = compositeLogListener;
            Task.Factory.StartNew(LoggingTask, TaskCreationOptions.LongRunning);
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

            _signal.WaitOne(TimeoutMilliseconds);
            _inner.RemoveListener(listener);
        }

        public void RemoveAllListeners()
        {
            _signal.WaitOne(TimeoutMilliseconds);
            _inner.RemoveAllListeners();
        }

        public void WriteLine(ILogItem item)
        {
            _signal.Reset();
            _logQueue.Add(item);
        }

        public void Flush()
        {
            _signal.WaitOne(TimeoutMilliseconds);
            _inner.Flush();
        }

        public void Dispose()
        {
            _signal.WaitOne(TimeoutMilliseconds);
            _inner.Dispose();
            _logQueue.CompleteAdding();
        }

        private void LoggingTask()
        {
            while (true)
            {
                if (_logQueue.Count == 0)
                {
                    _signal.Set();
                }

                if (_logQueue.IsCompleted)
                {
                    break;
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

            _logQueue.Dispose();
        }

        private void LogCore(ILogItem item)
        {
            _inner.WriteLine(item);
        }
    }
}