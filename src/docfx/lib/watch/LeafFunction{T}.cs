// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build
{
    internal class LeafFunction<T> : IFunction
    {
        private readonly Func<T> _valueFactory;

        [MaybeNull]
        internal T Result { get; set; }

        public LeafFunction(Func<T> valueFactory) => _valueFactory = valueFactory;

        public bool HasChanged() => !Equals(Result, _valueFactory());

        public void AddChild(IFunction childFunction) { }
    }
}
