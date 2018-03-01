// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    /// <summary>
    /// A type like <see cref="System.Text.StringBuilder"/> but only do concat.
    /// </summary>
    public sealed class StringBuffer
    {
        private const int MinArrayLength = 8;
        private const int ShrinkArrayLength = 100;

        /// <summary>
        /// An empty <see cref="StringBuffer"/>.
        /// </summary>
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
            _buffer = new string[MinArrayLength];
            _buffer[0] = value;
            _index = 1;
        }

        /// <summary>
        /// Append string to string buffer. (Create new instance if self is <see cref="Empty"/>, otherwise, modify self).
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The string buffer.</returns>
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

        /// <summary>
        /// Concat another string buffer. (Create new instance if self is <see cref="Empty"/>, otherwise, modify self).
        /// </summary>
        /// <param name="another">The string buffer.</param>
        /// <returns>The string buffer.</returns>
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
            if (another._index > ShrinkArrayLength)
            {
                var result = EnsureCapacity(1);
                result._buffer[result._index++] = another.ToString();
                return result;
            }
            else
            {
                var result = EnsureCapacity(another._index);
                Array.Copy(another._buffer, 0, result._buffer, result._index, another._index);
                result._index += another._index;
                return result;
            }
        }

        private StringBuffer EnsureCapacity(int count)
        {
            const int LobSize = 85000;
            if (this == Empty)
            {
                return new StringBuffer(Math.Max(count, MinArrayLength));
            }
            if (_index > ShrinkArrayLength)
            {
                if (_buffer[0].Length > LobSize / 3 &&
                    _buffer[1].Length < LobSize / 3)
                {
                    var temp = new string[_index - 1];
                    Array.Copy(_buffer, 1, temp, 0, _index - 1);
                    _buffer[1] = string.Concat(temp);
                    Array.Clear(_buffer, 2, _buffer.Length - 2);
                    _index = 2;
                }
                else
                {
                    _buffer[0] = string.Concat(_buffer);
                    Array.Clear(_buffer, 1, _buffer.Length - 1);
                    _index = 1;
                }
            }
            var expected = _index + count;
            if (expected > _buffer.Length)
            {
                var newLength = Math.Max(expected + MinArrayLength, _buffer.Length * 2);
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
            var result = new StringBuffer(_index + MinArrayLength);
            Array.Copy(_buffer, result._buffer, _index);
            result._index = _index;
            return result;
        }

        public int GetLength()
        {
            int result = 0;
            for (int i = 0; i < _index; i++)
            {
                result += _buffer[i].Length;
            }
            return result;
        }

        public bool StartsWith(char character)
        {
            if (_index == 0)
            {
                return false;
            }
            var lastStr = _buffer[0];
            return lastStr[0] == character;
        }

        public bool StartsWith(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (text.Length == 0)
            {
                return true;
            }
            if (_index == 0)
            {
                return false;
            }
            var index = 0;
            var current = _buffer[index];
            int j = 0;
            for (int i = 0; i < text.Length; i++, j++)
            {
                if (j >= current.Length)
                {
                    index++;
                    if (index >= _index)
                    {
                        return false;
                    }
                    current = _buffer[index];
                    j = 0;
                }
                if (text[i] != current[j])
                {
                    return false;
                }
            }
            return true;
        }

        public bool EndsWith(char character)
        {
            if (_index == 0)
            {
                return false;
            }
            var lastStr = _buffer[_index - 1];
            return lastStr[lastStr.Length - 1] == character;
        }

        public bool EndsWith(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (text.Length == 0)
            {
                return true;
            }
            if (_index == 0)
            {
                return false;
            }
            var index = _index - 1;
            string current = _buffer[index];
            int j = current.Length - 1;
            for (int i = text.Length - 1; i >= 0; i--, j--)
            {
                if (j < 0)
                {
                    index--;
                    if (index < 0)
                    {
                        return false;
                    }
                    current = _buffer[index];
                    j = current.Length - 1;
                }
                if (text[i] != current[j])
                {
                    return false;
                }
            }
            return true;
        }

        public StringBuffer Substring(int startIndex, int maxCount)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
            if (maxCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }
            if (maxCount == 0)
            {
                return Empty;
            }
            var result = new StringBuffer(_buffer.Length);
            int skipCount = startIndex;
            int copyCount = maxCount;
            for (int i = 0; i < _index; i++)
            {
                if (skipCount > 0)
                {
                    if (skipCount < _buffer[i].Length)
                    {
                        result._index = 1;
                        if (_buffer[i].Length - skipCount > copyCount)
                        {
                            result._buffer[0] = _buffer[i].Substring(skipCount, copyCount);
                            return result;
                        }
                        else
                        {
                            result._buffer[0] = _buffer[i].Substring(skipCount);
                            copyCount -= result._buffer[0].Length;
                        }
                    }
                    skipCount -= _buffer[i].Length;
                }
                else
                {
                    if (copyCount > _buffer[i].Length)
                    {
                        result._buffer[result._index++] = _buffer[i];
                        copyCount -= _buffer[i].Length;
                    }
                    else
                    {
                        result._buffer[result._index++] = copyCount == _buffer[i].Length ? _buffer[i] : _buffer[i].Remove(copyCount);
                        return result;
                    }
                }
            }
            if (skipCount > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
            return result;
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

        public static implicit operator string(StringBuffer buffer)
        {
            return buffer.ToString();
        }
    }
}
