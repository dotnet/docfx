// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;

    public class CompositeLogListener : ILoggerListener
    {
        private readonly object _sync = new object();
        private readonly List<ILoggerListener> _listeners = new List<ILoggerListener>();

        public CompositeLogListener()
        {
        }

        public CompositeLogListener(IEnumerable<ILoggerListener> listeners)
        {
            _listeners.AddRange(listeners);
        }

        public int Count => _listeners.Count;

        public void AddListener(ILoggerListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            lock (_sync)
            {
                _listeners.Add(listener);
            }
        }

        public void AddListeners(IEnumerable<ILoggerListener> listeners)
        {
            if (listeners == null)
            {
                throw new ArgumentNullException(nameof(listeners));
            }

            lock (_sync)
            {
                _listeners.AddRange(listeners);
            }
        }

        public ILoggerListener FindListener(Predicate<ILoggerListener> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            lock (_sync)
            {
                return _listeners.Find(predicate);
            }
        }

        public void RemoveListener(ILoggerListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            lock (_sync)
            {
                listener.Dispose();
                // prevent marshal listener.
                _listeners.RemoveAll(l => listener.Equals(l));
            }
        }

        public void RemoveAllListeners()
        {
            lock (_sync)
            {
                foreach (var i in _listeners)
                {
                    i.Dispose();
                }

                _listeners.Clear();
            }
        }

        public void WriteLine(ILogItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            lock (_sync)
            {
                foreach (var listener in _listeners)
                {
                    listener.WriteLine(item);
                }
            }
        }

        public void Flush()
        {
            lock (_sync)
            {
                foreach (var listener in _listeners)
                {
                    listener.Flush();
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                foreach (var listener in _listeners)
                {
                    listener.Flush();
                }
            }
        }
    }
}
