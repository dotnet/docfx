// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;

    public class SharedObjectNode<TState, TOperand>
    {
        private readonly SharedObjectManager<TState, TOperand> _manager;
        private readonly ConcurrentDictionary<TOperand, SharedObjectNode<TState, TOperand>> _path;
        private readonly Func<TOperand, SharedObjectNode<TState, TOperand>> _creator;

        internal SharedObjectNode(SharedObjectManager<TState, TOperand> obj, TState value)
        {
            _manager = obj;
            _path = new ConcurrentDictionary<TOperand, SharedObjectNode<TState, TOperand>>(_manager.OperandComparer);
            Value = value;
            _creator = o => _manager.Modify(Value, o);
        }

        public TState Value { get; }

        public SharedObjectNode<TState, TOperand> Modify(TOperand operand) =>
            _path.GetOrAdd(operand, _creator);
    }
}
