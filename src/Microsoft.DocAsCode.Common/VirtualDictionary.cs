// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    [Serializable]
    public class VirtualDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _inner;

        public VirtualDictionary(): this(0, null) { }

        public VirtualDictionary(int capacity): this(capacity, null) { }

        public VirtualDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            _inner = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        public VirtualDictionary(IDictionary<TKey, TValue> dictionary): this(dictionary, null) { }

        public VirtualDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer):
            this(dictionary != null? dictionary.Count: 0, comparer)
        { }

        public TValue this[TKey key]
        {
            get
            {
                return _inner[key];
            }

            set
            {
                _inner[key] = value;
            }
        }

        public int Count
        {
            get
            {
                return _inner.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return _inner.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return _inner.Values;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _inner.Add(item.Key, item.Value);
        }

        public virtual void Add(TKey key, TValue value)
        {
            _inner.Add(key, value);
        }

        public virtual void Clear()
        {
            _inner.Clear();
        }

        public virtual bool ContainsKey(TKey key)
        {
            return _inner.ContainsKey(key);
        }

        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        public virtual bool Remove(TKey key)
        {
            return _inner.Remove(key);
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            return _inner.TryGetValue(key, out value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
