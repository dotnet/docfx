// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class SharedObjectManager<TState, TEvent>
    {
        private readonly ConcurrentDictionary<TState, SharedObjectNode<TState, TEvent>> _states;
        private readonly IEqualityComparer<TState> _stateComparer;
        private readonly Func<TState, TEvent, TState> _transit;
        private readonly Func<TState, SharedObjectNode<TState, TEvent>> _creator;

        public SharedObjectManager(
            TState initialState,
            Func<TState, TEvent, TState> transit,
            IEqualityComparer<TState> stateComparer = null,
            IEqualityComparer<TEvent> eventComparer = null)
        {
            _stateComparer = stateComparer ?? EqualityComparer<TState>.Default;
            _transit = transit ?? throw new ArgumentNullException(nameof(transit));
            _creator = CreateNewNode;
            EventComparer = eventComparer ?? EqualityComparer<TEvent>.Default;
            _states = new ConcurrentDictionary<TState, SharedObjectNode<TState, TEvent>>(_stateComparer);
            Node = CreateNewNode(initialState);
            _states.TryAdd(initialState, Node);
        }

        public SharedObjectNode<TState, TEvent> Node { get; }

        internal IEqualityComparer<TEvent> EventComparer { get; }

        internal SharedObjectNode<TState, TEvent> Transit(TState value, TEvent @event) =>
            _states.GetOrAdd(_transit(value, @event), _creator);

        private SharedObjectNode<TState, TEvent> CreateNewNode(TState value) =>
            new SharedObjectNode<TState, TEvent>(this, value);
    }
}
