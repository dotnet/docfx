// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Collections.Generic
{
    internal struct ArrayBuilder<T>
    {
        private T[]? _array;
        private int _length;

        public int Length => _length;

        public ReadOnlySpan<T> Span => _array is null ? ReadOnlySpan<T>.Empty : _array.AsSpan(0, _length);

        public void Add(T item)
        {
            _array ??= new T[4];

            if (_length == _array.Length)
            {
                var newArray = new T[_array.Length * 2];

                Array.Copy(_array, newArray, _length);

                _array = newArray;
            }

            _array[_length++] = item;
        }

        public void Clear()
        {
            _length = 0;
        }
    }
}
