// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;

    public class CircularBuffer<T>
    {
        private T[] _buffer;
        private int _index;
        private int _count;

        public CircularBuffer()
            : this(0x10)
        {
        }

        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
        }

        public void Write(T item)
        {
            EnsureCapacity(1);
            _buffer[WriteIndex] = item;
            _count++;
        }

        public void Write(T[] items)
        {
            Write(items, 0, items.Length);
        }

        public void Write(T[] items, int startIndex, int count)
        {
            EnsureCapacity(count);
            var c = _buffer.Length - WriteIndex;
            if (c >= count)
            {
                Array.Copy(items, startIndex, _buffer, WriteIndex, count);
            }
            else
            {
                Array.Copy(items, startIndex, _buffer, WriteIndex, c);
                Array.Copy(items, startIndex + c, _buffer, 0, count - c);
            }
            _count += count;
        }

        public T Read()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("No item to read.");
            }
            var result = _buffer[_index];
            _index = ++_index % _buffer.Length;
            _count--;
            return result;
        }

        public int Read(T[] buffer, int startIndex, int count)
        {
            var read = Math.Min(count, _count);
            var c = Math.Min(read, _buffer.Length - _index);
            Array.Copy(_buffer, _index, buffer, startIndex, c);
            if (c < read)
            {
                Array.Copy(_buffer, 0, buffer, startIndex + c, read - c);
            }
            _index = (_index + read) % _buffer.Length;
            _count -= read;
            return read;
        }

        public int Count => _count;

        private int WriteIndex => (_index + _count) % _buffer.Length;

        private void EnsureCapacity(int count)
        {
            var c = _count + count;
            if (c > _buffer.Length)
            {
                Resize(Math.Max(c, _buffer.Length * 2));
            }
        }

        private void Resize(int capacity)
        {
            var buffer = new T[capacity];
            if (_index + _count <= _buffer.Length)
            {
                Array.Copy(_buffer, _index, buffer, 0, _count);
            }
            else
            {
                var c = _buffer.Length - _index;
                Array.Copy(_buffer, _index, buffer, 0, c);
                Array.Copy(_buffer, 0, buffer, c, _count - c);
            }
            _buffer = buffer;
            _index = 0;
        }
    }
}
