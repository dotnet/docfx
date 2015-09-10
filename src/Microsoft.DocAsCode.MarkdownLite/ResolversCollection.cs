// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class ResolversCollection<T> : IEnumerable<T> where T: IResolver
    {
        private List<T> _resolvers = new List<T>();
        private Dictionary<string, int> _cache = new Dictionary<string, int>();

        public IEnumerable<string> ResolverNames { get { return _resolvers.Select(s => s.Name); } }

        public int Count { get { return _resolvers.Count; } }

        public ResolversCollection(IList<T> array = null)
        {
            if (array != null)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    _resolvers.Add(array[i]);
                    _cache.Add(array[i].Name, i);
                }
            }
        }

        public int GetIndex(string key)
        {
            int index;
            if (_cache.TryGetValue(key, out index))
            {
                return index;
            }

            return -1;
        }

        public void InsertAfter(T source, T item)
        {
            var index = GetIndex(source.Name);
            if (index == -1) throw new ArgumentException($"Resolver {source.Name} does not exist.");
            Insert(index + 1, item);
        }

        public void InsertBefore(T source, T item)
        {
            var index = GetIndex(source.Name);
            if (index == -1) throw new ArgumentException($"Resolver {source.Name} does not exist.");
            Insert(index, item);
        }

        public void Insert(int index, T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            try
            {
                _cache.Add(item.Name, index);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"{item.Name} already exists in the resolver, duplicate Name is not allowed!");
            }

            _resolvers.Insert(index, item);

            // Update index in dictionary
            for (int i = index + 1; i < Count; i++)
            {
                var key = _resolvers[i];
                _cache[key.Name]++;
            }
        }

        public void Add(T item)
        {
            Insert(Count, item);
        }

        public T this[string key]
        {
            get { return _resolvers[_cache[key]]; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _resolvers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _resolvers.GetEnumerator();
        }
    }

    public interface IResolver
    {
        string Name { get; }
    }
}
