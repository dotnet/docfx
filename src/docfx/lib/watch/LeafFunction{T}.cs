// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build
{
    internal class LeafFunction<T> : IFunction
    {
        private readonly Func<T> _changeTokenFactory;

        [MaybeNull]
        internal T ChangeToken { get; set; }

        public LeafFunction(Func<T> changeTokenFactory) => _changeTokenFactory = changeTokenFactory;

        public bool MayChange() => true;

        public bool HasChanged() => !Equals(ChangeToken, _changeTokenFactory());

        public void AddChild(IFunction childFunction) { }
    }
}
