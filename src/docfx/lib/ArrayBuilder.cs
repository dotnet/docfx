// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Collections.Concurrent
{
    internal class ArrayBuilder<T> : IEnumerable<T>
    {
        private readonly List<T> _array = new List<T>();

        public void Add(T item)
        {
            lock (_array)
            {
                _array.Add(item);
            }
        }

        public IEnumerator<T> GetEnumerator()
            => _array.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _array.GetEnumerator();
    }
}
