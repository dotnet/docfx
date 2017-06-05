// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class SharedObjectManager<TState, TEvent>
    {
        private readonly Func<TState, SharedObjectNode<TState, TEvent>> _creator;
        private readonly Func<TState, TEvent, TState> _transit;
        private readonly ConcurrentDictionary<TState, SharedObjectNode<TState, TEvent>> _states;

        public SharedObjectManager(
            TState initialState,
            Func<TState, TEvent, TState> transit,
            IEqualityComparer<TState> stateComparer = null,
            IEqualityComparer<TEvent> eventComparer = null)
        {
            _creator = CreateNewNode;
            _transit = transit ?? throw new ArgumentNullException(nameof(transit));
            _states = new ConcurrentDictionary<TState, SharedObjectNode<TState, TEvent>>(stateComparer ?? EqualityComparer<TState>.Default);
            EventComparer = eventComparer ?? EqualityComparer<TEvent>.Default;
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
