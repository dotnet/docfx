// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Collections.Concurrent
{
    internal class DictionaryBuilder<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public bool TryAdd(TKey key, TValue value)
        {
            lock (_dictionary)
            {
                return _dictionary.TryAdd(key, value);
            }
        }

        public IReadOnlyDictionary<TKey, TValue> ToDictionary()
            => _dictionary;
    }
}
