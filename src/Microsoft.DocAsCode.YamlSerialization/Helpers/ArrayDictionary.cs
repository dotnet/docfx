// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.YamlSerialization.Helpers
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Array based key-value cache.
    /// *Optimized for small size.*
    /// All method is thread safe.
    /// </summary>
    internal sealed class ArrayDictionary<TKey, TValue>
    {
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly object _syncRoot = new object();
        private volatile KeyValuePair<TKey, TValue>[] _cache = null;

        public ArrayDictionary(IEqualityComparer<TKey> comparer = null)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return TryFindInCache(_cache, key, out value);
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
        {
            var cache = _cache;
            if (TryFindInCache(cache, key, out TValue value))
            {
                return value;
            }
            lock (_syncRoot)
            {
                var syncCache = _cache;
                if (syncCache != cache)
                {
                    if (TryFindInCache(syncCache, key, out value, cache?.Length ?? 0))
                    {
                        return value;
                    }
                }
                value = func(key);
                if (syncCache == null)
                {
                    _cache = new KeyValuePair<TKey, TValue>[]
                    {
                        new KeyValuePair<TKey, TValue>(key, value)
                    };
                }
                else
                {
                    var temp = new KeyValuePair<TKey, TValue>[syncCache.Length + 1];
                    Array.Copy(syncCache, temp, syncCache.Length);
                    temp[temp.Length - 1] = new KeyValuePair<TKey, TValue>(key, value);
                    _cache = temp;
                }
                return value;
            }
        }

        private bool TryFindInCache(KeyValuePair<TKey, TValue>[] cache, TKey key, out TValue value, int startIndex = 0)
        {
            if (cache != null)
            {
                for (int i = startIndex; i < cache.Length; i++)
                {
                    if (_comparer.Equals(cache[i].Key, key))
                    {
                        value = cache[i].Value;
                        return true;
                    }
                }
            }
            value = default(TValue);
            return false;
        }
    }
}
