// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build;

[DebuggerTypeProxy(typeof(WatchDebugView<>))]
[DebuggerDisplay("ChangeCount={ChangeCount}, Value={ValueForDebugDisplay}")]
public class Watch<T>
{
    private readonly Func<T> _valueFactory;
    private readonly object _syncLock = new();

    private T? _value;
    private int _changeCount;

    private volatile WatchFunction? _function;

    public Watch(Func<T> valueFactory) => _valueFactory = valueFactory;

    public int ChangeCount => _changeCount;

    public override string? ToString() => _function != null ? _value?.ToString() : base.ToString();

    public T Value
    {
        get
        {
            if (TryGetValue(out var value))
            {
                return value;
            }

            if (Monitor.IsEntered(_syncLock))
            {
                throw new InvalidOperationException("ValueFactory attempted to access the Value property of this instance.");
            }

            lock (_syncLock)
            {
                if (TryGetValue(out value))
                {
                    return value;
                }

                if (Watcher.IsDisabled)
                {
                    _value = _valueFactory();
                    _changeCount++;
                    return _value;
                }

                _changeCount++;

                var function = new WatchFunction();

                Watcher.BeginFunctionScope(function);

                try
                {
                    _value = _valueFactory();
                    _function = function;
                    return _value!;
                }
                finally
                {
                    Watcher.EndFunctionScope(attachToParent: function.HasChildren);
                }
            }
        }
    }

    internal T? ValueForDebugDisplay => _value;

    private bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        if (Watcher.IsDisabled)
        {
            value = _value;
            return _changeCount > 0;
        }

        var currentFunction = _function;
        if (currentFunction != null && !currentFunction.HasChanged())
        {
            Watcher.AttachToParent(currentFunction);
            currentFunction.Replay();
            value = _value!;
            return true;
        }

        value = default;
        return false;
    }
}
