// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.Docs.Build
{
    public class Watch<T> : IFunction
    {
        private readonly Func<T> _valueFactory;

        [MaybeNull]
        private T _value;
        private bool _hasChanged;
        private int _activityId = -1;

        private ContainerFunction? _function;
        private object? _syncLock;

        public Watch(Func<T> valueFactory) => _valueFactory = valueFactory;

        public bool IsValueCreated => _function != null;

        public override string? ToString() => _function != null ? _value?.ToString() : base.ToString();

        public T Value
        {
            get
            {
                EnsureValue(Watcher.GetActivityId());
                return _value!;
            }
        }

        bool IFunction.HasChanged(int activityId)
        {
            EnsureValue(activityId);
            return _hasChanged;
        }

        void IFunction.AddChild(IFunction child) => throw new InvalidOperationException();

        private void EnsureValue(int activityId)
        {
            if (activityId != _activityId)
            {
                if (_function is null || _function.HasChanged(activityId))
                {
                    CreateValue();
                }
                else
                {
                    _hasChanged = false;
                }

                Volatile.Write(ref _activityId, activityId);
            }

            if (_function != null && _function.HasChildren)
            {
                Watcher.AttachToParent(this);
            }
        }

        private void CreateValue()
        {
            var function = new ContainerFunction();

            Watcher.BeginFunctionScope(function);

            try
            {
                if (_syncLock != null && Monitor.IsEntered(_syncLock))
                {
                    throw new InvalidOperationException("ValueFactory attempted to access the Value property of this instance.");
                }

                lock (EnsureLock(ref _syncLock))
                {
                    var newValue = _valueFactory();
                    _hasChanged = !Equals(_value, newValue);
                    _value = newValue;
                    _function = function;
                }
            }
            finally
            {
                Watcher.EndFunctionScope();
            }
        }

        private static object EnsureLock(ref object? syncLock)
        {
            return syncLock ?? Interlocked.CompareExchange(ref syncLock, new object(), null) ?? syncLock;
        }
    }
}
