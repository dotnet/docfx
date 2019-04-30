// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Collections.Concurrent
{
    internal class DictionaryBuilder<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => _dictionary.GetEnumerator();

        public int Count => _dictionary.Count;

        public bool TryAdd(TKey key, TValue value)
        {
            lock (_dictionary)
            {
                return _dictionary.TryAdd(key, value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => _dictionary.GetEnumerator();

        public IReadOnlyDictionary<TKey, TValue> ToDictionary()
            => _dictionary;

        public bool TryGetValue(TKey key, out TValue value)
            => _dictionary.TryGetValue(key, out value);
    }
}
