// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;

    public class StateNode<TState, TEvent>
    {
        private readonly StateMachine<TState, TEvent> _manager;
        private readonly ConcurrentDictionary<TEvent, StateNode<TState, TEvent>> _path;
        private readonly Func<TEvent, StateNode<TState, TEvent>> _creator;

        internal StateNode(StateMachine<TState, TEvent> obj, TState value)
        {
            _manager = obj;
            _path = new ConcurrentDictionary<TEvent, StateNode<TState, TEvent>>(_manager.EventComparer);
            Value = value;
            _creator = c => _manager.Transit(Value, c);
        }

        public TState Value { get; }

        public StateNode<TState, TEvent> Transit(TEvent @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event));
            }
            return _path.GetOrAdd(@event, _creator);
        }
    }
}
