// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.Docs.Build
{
    public class Watch<T>
    {
        private readonly Func<T> _valueFactory;
        private readonly object _syncLock = new object();

        private T? _value;

        private volatile WatchFunction? _function;

        public Watch(Func<T> valueFactory) => _valueFactory = valueFactory;

        public bool IsValueCreated => _function != null;

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

        private bool TryGetValue([NotNullWhen(true)] out T? value)
        {
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
}
