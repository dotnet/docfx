// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;

    public class SharedObjectNode<TState, TEvent>
    {
        private readonly SharedObjectManager<TState, TEvent> _manager;
        private readonly ConcurrentDictionary<TEvent, SharedObjectNode<TState, TEvent>> _path;
        private readonly Func<TEvent, SharedObjectNode<TState, TEvent>> _creator;

        internal SharedObjectNode(SharedObjectManager<TState, TEvent> manager, TState value)
        {
            _manager = manager;
            _path = new ConcurrentDictionary<TEvent, SharedObjectNode<TState, TEvent>>(_manager.EventComparer);
            Value = value;
            _creator = c => _manager.Transit(Value, c);
        }

        public TState Value { get; }

        public SharedObjectNode<TState, TEvent> Transit(TEvent @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }
            return _path.GetOrAdd(@event, _creator);
        }
    }
}
