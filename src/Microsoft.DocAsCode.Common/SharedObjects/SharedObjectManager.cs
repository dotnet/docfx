// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    public class SharedObjectManager<TState, TOperand>
    {
        private readonly ConcurrentDictionary<TState, SharedObjectNode<TState, TOperand>> _states;
        private readonly IEqualityComparer<TState> _stateComparer;
        private readonly Func<TState, TOperand, TState> _modifier;
        private readonly Func<TState, SharedObjectNode<TState, TOperand>> _creator;

        public SharedObjectManager(
            TState rootState,
            Func<TState, TOperand, TState> modifier,
            IEqualityComparer<TState> stateComparer = null,
            IEqualityComparer<TOperand> operandComparer = null)
        {
            _stateComparer = stateComparer ?? EqualityComparer<TState>.Default;
            _modifier = modifier;
            _creator = CreateNewNode;
            OperandComparer = operandComparer ?? EqualityComparer<TOperand>.Default;
            _states = new ConcurrentDictionary<TState, SharedObjectNode<TState, TOperand>>(_stateComparer);
            RootState = new SharedObjectNode<TState, TOperand>(this, rootState);
            _states.TryAdd(rootState, RootState);
        }

        public SharedObjectNode<TState, TOperand> RootState { get; }

        internal IEqualityComparer<TOperand> OperandComparer { get; }

        internal SharedObjectNode<TState, TOperand> Modify(TState value, TOperand operand) =>
            _states.GetOrAdd(_modifier(value, operand), _creator);

        private SharedObjectNode<TState, TOperand> CreateNewNode(TState value) =>
            new SharedObjectNode<TState, TOperand>(this, value);
    }
}
