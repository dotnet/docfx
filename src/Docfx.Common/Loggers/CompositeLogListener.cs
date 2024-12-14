// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public class CompositeLogListener : ILoggerListener
{
    private readonly object _sync = new();
    private readonly List<ILoggerListener> _listeners = [];

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
        ArgumentNullException.ThrowIfNull(listener);

        lock (_sync)
        {
            _listeners.Add(listener);
        }
    }

    public void AddListeners(IEnumerable<ILoggerListener> listeners)
    {
        ArgumentNullException.ThrowIfNull(listeners);

        lock (_sync)
        {
            _listeners.AddRange(listeners);
        }
    }

    public IEnumerable<ILoggerListener> GetAllListeners()
    {
        lock (_sync)
        {
            return _listeners.ToArray();
        }
    }

    public ILoggerListener FindListener(Predicate<ILoggerListener> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        lock (_sync)
        {
            return _listeners.Find(predicate);
        }
    }

    public void RemoveListener(ILoggerListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        lock (_sync)
        {
            listener.Dispose();
            // prevent marshal listener.
            _listeners.RemoveAll(listener.Equals);
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
        ArgumentNullException.ThrowIfNull(item);

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
