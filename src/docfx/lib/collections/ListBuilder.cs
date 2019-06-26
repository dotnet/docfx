// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Collections.Concurrent
{
    internal class ListBuilder<T>
    {
        private readonly List<T> _array = new List<T>();

        public void Add(T item)
        {
            lock (_array)
            {
                _array.Add(item);
            }
        }

        public void AddRange(IReadOnlyList<T> items)
        {
            lock (_array)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    _array.Add(items[i]);
                }
            }
        }

        public IReadOnlyList<T> ToList() => _array;
    }
}
