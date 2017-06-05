// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    public class StateMachine<TState, TEvent>
    {
        private readonly ConcurrentDictionary<TState, StateNode<TState, TEvent>> _states;
        private readonly IEqualityComparer<TState> _stateComparer;
        private readonly Func<TState, TEvent, TState> _transit;
        private readonly Func<TState, StateNode<TState, TEvent>> _creator;

        public StateMachine(
            TState rootState,
            Func<TState, TEvent, TState> transit,
            IEqualityComparer<TState> stateComparer = null,
            IEqualityComparer<TEvent> eventComparer = null)
        {
            _stateComparer = stateComparer ?? EqualityComparer<TState>.Default;
            _transit = transit;
            _creator = CreateNewNode;
            EventComparer = eventComparer ?? EqualityComparer<TEvent>.Default;
            _states = new ConcurrentDictionary<TState, StateNode<TState, TEvent>>(_stateComparer);
            RootState = new StateNode<TState, TEvent>(this, rootState);
            _states.TryAdd(rootState, RootState);
        }

        public StateNode<TState, TEvent> RootState { get; }

        internal IEqualityComparer<TEvent> EventComparer { get; }

        internal StateNode<TState, TEvent> Transit(TState value, TEvent @event) =>
            _states.GetOrAdd(_transit(value, @event), _creator);

        private StateNode<TState, TEvent> CreateNewNode(TState value) =>
            new StateNode<TState, TEvent>(this, value);
    }
}
