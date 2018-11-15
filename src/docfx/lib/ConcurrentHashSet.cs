// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Concurrent
{
    internal class ConcurrentHashSet<T>
    {
        private readonly ConcurrentDictionary<T, object> _dictionary;

        public ConcurrentHashSet() => _dictionary = new ConcurrentDictionary<T, object>();

        public ConcurrentHashSet(IEnumerable<T> source) => _dictionary = new ConcurrentDictionary<T, object>(source.Select(item => new KeyValuePair<T, object>(item, default)));

        public ConcurrentHashSet(IEqualityComparer<T> comparer) => _dictionary = new ConcurrentDictionary<T, object>(comparer);

        public bool TryAdd(T value) => _dictionary.TryAdd(value, null);

        public bool Contains(T value) => _dictionary.ContainsKey(value);
    }
}
