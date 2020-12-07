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
        private int _valueActivityId = -1;

        private bool _hasChanged;
        private int _hasChangedActivityId = -1;

        private ContainerFunction? _function;
        private object? _syncLock;

        public Watch(Func<T> valueFactory) => _valueFactory = valueFactory;

        public bool IsValueCreated => _function != null;

        public override string? ToString() => _function != null ? _value?.ToString() : base.ToString();

        bool IFunction.HasChanged(int activityId) => HasChanged(activityId);

        void IFunction.AddChild(IFunction child) => throw new InvalidOperationException();

        public T Value
        {
            get
            {
                var activityId = Watcher.GetActivityId();
                if (activityId == _valueActivityId)
                {
                    Watcher.AttachToParent(this);
                    return _value!;
                }

                if (HasChanged(activityId))
                {
                    CreateValue();
                }

                Volatile.Write(ref _valueActivityId, activityId);
                return _value!;
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
                    _value = _valueFactory();
                    _function = function;
                }
            }
            finally
            {
                Watcher.EndFunctionScope();
                Watcher.AttachToParent(this);
            }
        }

        private static object EnsureLock(ref object? syncLock)
        {
            return syncLock ?? Interlocked.CompareExchange(ref syncLock, new object(), null) ?? syncLock;
        }

        private bool HasChanged(int activityId)
        {
            if (activityId != _hasChangedActivityId)
            {
                var hasChanged = _function is null || _function.HasChanged(activityId);
                _hasChanged = hasChanged;

                Volatile.Write(ref _hasChangedActivityId, activityId);
            }

            return _hasChanged;
        }
    }
}
