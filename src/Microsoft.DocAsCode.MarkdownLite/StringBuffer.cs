// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    public sealed class StringBuffer : ICloneable
    {
        public static readonly StringBuffer Empty = new StringBuffer(0);
        private string[] _buffer;
        private int _index;

        private StringBuffer(int length)
        {
            _buffer = new string[length];
            _index = 0;
        }

        private StringBuffer(string value)
        {
            _buffer = new string[8];
            _buffer[0] = value;
            _index = 1;
        }

        public StringBuffer Append(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return this;
            }
            var result = EnsureCapacity(1);
            result._buffer[result._index++] = str;
            return result;
        }

        public StringBuffer Concat(StringBuffer another)
        {
            if (another == Empty || another == null)
            {
                return this;
            }
            if (this == Empty)
            {
                return another;
            }
            var result = EnsureCapacity(another._index);
            Array.Copy(another._buffer, 0, result._buffer, result._index, another._index);
            result._index += another._index;
            return result;
        }

        private StringBuffer EnsureCapacity(int count)
        {
            if (this == Empty)
            {
                return new StringBuffer(Math.Max(count, 8));
            }
            if (_index > 100)
            {
                _buffer[0] = string.Concat(_buffer);
                Array.Clear(_buffer, 1, _buffer.Length - 1);
                _index = 1;
            }
            var expected = _index + count;
            if (expected > _buffer.Length)
            {
                var newLength = Math.Max(expected + 8, _buffer.Length * 2);
                var temp = new string[newLength];
                Array.Copy(_buffer, temp, _buffer.Length);
                _buffer = temp;
            }
            return this;
        }

        public StringBuffer Clone()
        {
            if (this == Empty)
            {
                return this;
            }
            var result = new StringBuffer(_index + 8);
            Array.Copy(_buffer, result._buffer, _index);
            result._index = _index;
            return result;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public override string ToString()
        {
            if (this == Empty)
            {
                return string.Empty;
            }
            return string.Concat(_buffer);
        }

        public static StringBuffer operator +(StringBuffer buffer, string value)
        {
            return (buffer ?? Empty).Append(value);
        }

        public static StringBuffer operator +(StringBuffer buffer, StringBuffer another)
        {
            return (buffer ?? Empty).Concat(another);
        }

        public static implicit operator StringBuffer(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Empty;
            }
            return new StringBuffer(value);
        }

        public static implicit operator string (StringBuffer buffer)
        {
            return buffer.ToString();
        }
    }
}
