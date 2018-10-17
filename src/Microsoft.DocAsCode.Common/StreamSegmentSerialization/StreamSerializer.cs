// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.StreamSegmentSerialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class StreamSerializer
    {
        public Stream UnderlyingStream { get; }
        private readonly BinaryWriter _writer;

        public StreamSerializer(Stream underlyingStream)
        {
            UnderlyingStream = underlyingStream;
            _writer = new BinaryWriter(underlyingStream);
        }

        public StreamSegment WriteNull()
        {
            var startOffset = (int)UnderlyingStream.Position;
            _writer.Write(4 + 4 + 1); // length
            _writer.Write(startOffset + 4 + 4 + 1); // next
            _writer.Write((byte)StreamSegmentType.Null);
            return new StreamSegment(UnderlyingStream, startOffset, 4 + 4 + 1, startOffset + 4 + 4 + 1, StreamSegmentType.Null);
        }

        public StreamSegment Write(int value)
        {
            var startOffset = (int)UnderlyingStream.Position;
            _writer.Write(4 + 4 + 1 + 4); // length
            _writer.Write(startOffset + 4 + 4 + 1 + 4); // next
            _writer.Write((byte)StreamSegmentType.Integer);
            _writer.Write(value);
            return new StreamSegment(UnderlyingStream, startOffset, 4 + 4 + 1 + 4, startOffset + 4 + 4 + 1 + 4, StreamSegmentType.Integer);
        }

        public StreamSegment Write(string value)
        {
            if (value == null)
            {
                return WriteNull();
            }
            return WriteString(value);
        }

        public StreamSegment Write(byte[] value)
        {
            if (value == null)
            {
                return WriteNull();
            }
            return WriteBytes(value);
        }

        public StreamSegment Write(Action<Stream> writeAction)
        {
            if (writeAction == null)
            {
                throw new ArgumentNullException(nameof(writeAction));
            }
            // todo : forbidden seek.
            return WriteBytes(writeAction);
        }

        public StreamSegment Write(IReadOnlyList<object> value)
        {
            if (value == null)
            {
                return WriteNull();
            }
            return WriteArray(value);
        }

        public StreamSegment Write(IReadOnlyCollection<KeyValuePair<string, object>> value)
        {
            if (value == null)
            {
                return WriteNull();
            }
            return WriteDictionary(value);
        }

        public StreamSegment Write(object value)
        {
            if (value == null)
            {
                return WriteNull();
            }
            if (value is string s)
            {
                return WriteString(s);
            }
            if (value is byte[] bytes)
            {
                return WriteBytes(bytes);
            }
            if (value is int number)
            {
                return Write(number);
            }
            if (value is IReadOnlyCollection<object> list)
            {
                return WriteArray(list);
            }
            if (value is IReadOnlyCollection<KeyValuePair<string, object>> dict)
            {
                return WriteDictionary(dict);
            }
            throw new NotSupportedException(value.GetType().FullName);
        }

        private StreamSegment WriteString(string value)
        {
            var startOffset = (int)UnderlyingStream.Position;
            UnderlyingStream.Seek(4 + 4, SeekOrigin.Current);
            _writer.Write((byte)StreamSegmentType.String);
            _writer.Write(value);
            var next = (int)UnderlyingStream.Position;
            UnderlyingStream.Seek(startOffset, SeekOrigin.Begin);
            _writer.Write(next - startOffset); // length
            _writer.Write(next); // next
            UnderlyingStream.Seek(next, SeekOrigin.Begin);
            return new StreamSegment(UnderlyingStream, startOffset, next - startOffset, next, StreamSegmentType.String);
        }

        private StreamSegment WriteBytes(byte[] bytes)
        {
            return WriteBytes(s => s.Write(bytes, 0, bytes.Length));
        }

        private StreamSegment WriteBytes(Action<Stream> writeAction)
        {
            var startOffset = (int)UnderlyingStream.Position;
            UnderlyingStream.Seek(4 + 4, SeekOrigin.Current);
            _writer.Write((byte)StreamSegmentType.Binary);
            writeAction(UnderlyingStream);
            var next = (int)UnderlyingStream.Position;
            UnderlyingStream.Seek(startOffset, SeekOrigin.Begin);
            _writer.Write(next - startOffset); // length
            _writer.Write(next); // next
            UnderlyingStream.Seek(next, SeekOrigin.Begin);
            return new StreamSegment(UnderlyingStream, startOffset, next - startOffset, next, StreamSegmentType.Binary);
        }

        private StreamSegment WriteArray(IReadOnlyCollection<object> value)
        {
            var startOffset = (int)UnderlyingStream.Position;
            var length = 4 + 4 + 1 + (value.Count * 4);
            UnderlyingStream.Seek(length, SeekOrigin.Current);
            var indices = new List<int>(value.Count);
            foreach (var item in value)
            {
                indices.Add(Write(item).StartOffset);
            }
            var next = (int)UnderlyingStream.Position;
            UnderlyingStream.Seek(startOffset, SeekOrigin.Begin);
            _writer.Write(length); // length
            _writer.Write(next); // next
            _writer.Write((byte)StreamSegmentType.Array);
            foreach (var item in indices)
            {
                _writer.Write(item); // offsets for key and value
            }
            UnderlyingStream.Seek(next, SeekOrigin.Begin);
            return new StreamSegment(
                UnderlyingStream,
                startOffset,
                4 + 4 + 1 + (value.Count * 4),
                next,
                StreamSegmentType.Array);
        }

        private StreamSegment WriteDictionary(IReadOnlyCollection<KeyValuePair<string, object>> value)
        {
            var startOffset = (int)UnderlyingStream.Position;
            var length = 4 + 4 + 1 + (value.Count * 2 * 4);
            UnderlyingStream.Seek(length, SeekOrigin.Current);
            var indices = new List<int>(value.Count * 2);
            foreach (var pair in value)
            {
                indices.Add(Write(pair.Key).StartOffset);
                indices.Add(Write(pair.Value).StartOffset);
            }
            var next = (int)UnderlyingStream.Position;
            UnderlyingStream.Seek(startOffset, SeekOrigin.Begin);
            _writer.Write(length); // length
            _writer.Write(next); // next
            _writer.Write((byte)StreamSegmentType.Dictionary);
            foreach (var item in indices)
            {
                _writer.Write(item); // offsets for key and value
            }
            UnderlyingStream.Seek(next, SeekOrigin.Begin);
            return new StreamSegment(
                UnderlyingStream,
                startOffset,
                4 + 4 + 1 + (value.Count * 2 * 4),
                next,
                StreamSegmentType.Dictionary);
        }
    }
}
