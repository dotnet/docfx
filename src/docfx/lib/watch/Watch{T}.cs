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

        [MaybeNull]
        private T _value;

        private volatile ContainerFunction? _function;
        private object? _syncLock;

        public Watch(Func<T> valueFactory) => _valueFactory = valueFactory;

        public bool IsValueCreated => _function != null;

        public override string? ToString() => _function != null ? _value?.ToString() : base.ToString();

        public T Value
        {
            get
            {
                var function = _function;
                if (function != null && !function.HasChanged())
                {
                    Watcher.AttachToParent(function);
                    return _value!;
                }

                function = new ContainerFunction();

                Watcher.BeginFunctionScope(function);

                try
                {
                    if (_syncLock != null && Monitor.IsEntered(_syncLock))
                    {
                        throw new InvalidOperationException("ValueFactory attempted to access the Value property of this instance.");
                    }

                    lock (EnsureLock(ref _syncLock))
                    {
                        _value = _valueFactory();
                        _function = function;
                    }

                    return _value!;
                }
                finally
                {
                    Watcher.EndFunctionScope(attachToParent: function.HasChildren);
                }
            }
        }

        private static object EnsureLock(ref object? syncLock)
        {
            return syncLock ?? Interlocked.CompareExchange(ref syncLock, new object(), null) ?? syncLock;
        }
    }
}
