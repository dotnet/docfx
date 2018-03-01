// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.StreamSegmentSerialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class StreamDeserializer
    {
        public Stream UnderlyingStream { get; }
        private readonly BinaryReader _reader;

        public StreamDeserializer(Stream underlyingStream)
        {
            UnderlyingStream = underlyingStream;
            _reader = new BinaryReader(underlyingStream);
        }

        public object Read()
        {
            var seg = ReadSegment();
            var result = ReadValue(seg);
            UnderlyingStream.Seek(seg.Next, SeekOrigin.Begin);
            return result;
        }

        public StreamSegment ReadSegment()
        {
            var startOffset = (int)UnderlyingStream.Position;
            var length = _reader.ReadInt32();
            var next = _reader.ReadInt32();
            var type = (StreamSegmentType)_reader.ReadByte();
            return new StreamSegment(UnderlyingStream, startOffset, length, next, type);
        }

        public StreamSegment ReadSegment(int startOffset)
        {
            UnderlyingStream.Seek(startOffset, SeekOrigin.Begin);
            return ReadSegment();
        }

        public StreamSegment ReadNext(StreamSegment current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }
            if (current.Next >= UnderlyingStream.Length)
            {
                return null;
            }
            UnderlyingStream.Seek(current.Next, SeekOrigin.Begin);
            return ReadSegment();
        }

        public object ReadValue(StreamSegment current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }
            if (current.UnderlyingStream != UnderlyingStream)
            {
                throw new ArgumentException("Invalid argument.", nameof(current));
            }
            switch (current.ContentType)
            {
                case StreamSegmentType.Null:
                    return null;
                case StreamSegmentType.Integer:
                    return ReadIntegerCore(current);
                case StreamSegmentType.String:
                    return ReadStringCore(current);
                case StreamSegmentType.Binary:
                    return ReadBinaryCore(current);
                case StreamSegmentType.Dictionary:
                    return ReadDictionaryCore(current);
                case StreamSegmentType.Array:
                    return ReadArrayCore(current);
                default:
                    throw new InvalidDataException();
            }
        }

        public string ReadInteger(StreamSegment current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }
            if (current.UnderlyingStream != UnderlyingStream)
            {
                throw new ArgumentException("Invalid argument.", nameof(current));
            }
            switch (current.ContentType)
            {
                case StreamSegmentType.Integer:
                    return ReadStringCore(current);
                default:
                    throw new InvalidDataException();
            }
        }

        public string ReadString(StreamSegment current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }
            if (current.UnderlyingStream != UnderlyingStream)
            {
                throw new ArgumentException("Invalid argument.", nameof(current));
            }
            switch (current.ContentType)
            {
                case StreamSegmentType.Null:
                    return null;
                case StreamSegmentType.String:
                    return ReadStringCore(current);
                default:
                    throw new InvalidDataException();
            }
        }

        public byte[] ReadBinary(StreamSegment current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }
            if (current.UnderlyingStream != UnderlyingStream)
            {
                throw new ArgumentException("Invalid argument.", nameof(current));
            }
            switch (current.ContentType)
            {
                case StreamSegmentType.Null:
                    return null;
                case StreamSegmentType.Binary:
                    return ReadBinaryCore(current);
                default:
                    throw new InvalidDataException();
            }
        }

        public Stream ReadBinaryAsStream(StreamSegment current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }
            if (current.UnderlyingStream != UnderlyingStream)
            {
                throw new ArgumentException("Invalid argument.", nameof(current));
            }
            switch (current.ContentType)
            {
                case StreamSegmentType.Null:
                    return Stream.Null;
                case StreamSegmentType.Binary:
                    // todo : use underlying stream later.
                    return new MemoryStream(ReadBinaryCore(current));
                default:
                    throw new InvalidDataException();
            }
        }

        public object[] ReadArray(StreamSegment current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }
            if (current.UnderlyingStream != UnderlyingStream)
            {
                throw new ArgumentException("Invalid argument.", nameof(current));
            }
            switch (current.ContentType)
            {
                case StreamSegmentType.Null:
                    return null;
                case StreamSegmentType.Array:
                    return ReadArrayCore(current);
                default:
                    throw new InvalidDataException();
            }
        }

        public Dictionary<string, object> ReadDictionary(StreamSegment current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }
            if (current.UnderlyingStream != UnderlyingStream)
            {
                throw new ArgumentException("Invalid argument.", nameof(current));
            }
            switch (current.ContentType)
            {
                case StreamSegmentType.Null:
                    return null;
                case StreamSegmentType.Dictionary:
                    return ReadDictionaryCore(current);
                default:
                    throw new InvalidDataException();
            }
        }

        public Dictionary<string, Lazy<object>> ReadDictionaryLazy(StreamSegment current)
        {
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }
            if (current.UnderlyingStream != UnderlyingStream)
            {
                throw new ArgumentException("Invalid argument.", nameof(current));
            }
            switch (current.ContentType)
            {
                case StreamSegmentType.Null:
                    return null;
                case StreamSegmentType.Dictionary:
                    return ReadDictionaryLazyCore(current);
                default:
                    throw new InvalidDataException();
            }
        }

        #region Private Methods

        private int ReadIntegerCore(StreamSegment current)
        {
            UnderlyingStream.Seek(current.ContentStartOffset, SeekOrigin.Begin);
            return _reader.ReadInt32();
        }

        private string ReadStringCore(StreamSegment current)
        {
            UnderlyingStream.Seek(current.ContentStartOffset, SeekOrigin.Begin);
            return _reader.ReadString();
        }

        private byte[] ReadBinaryCore(StreamSegment current)
        {
            UnderlyingStream.Seek(current.ContentStartOffset, SeekOrigin.Begin);
            return _reader.ReadBytes(current.ContentLength);
        }

        private object[] ReadArrayCore(StreamSegment current)
        {
            UnderlyingStream.Seek(current.ContentStartOffset, SeekOrigin.Begin);
            var indices = new int[current.ContentLength / 4];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = _reader.ReadInt32();
            }
            var result = new object[current.ContentLength / 4];
            for (int i = 0; i < indices.Length; i++)
            {
                result[i] = ReadValue(ReadSegment(indices[i]));
            }
            return result;
        }

        private Dictionary<string, object> ReadDictionaryCore(StreamSegment current)
        {
            UnderlyingStream.Seek(current.ContentStartOffset, SeekOrigin.Begin);
            var indices = new int[current.ContentLength / 4];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = _reader.ReadInt32();
            }
            var result = new Dictionary<string, object>();
            for (int i = 0; i < indices.Length;)
            {
                var keySeg = ReadSegment(indices[i++]);
                var valueSeg = ReadSegment(indices[i++]);
                result[ReadString(keySeg)] = ReadValue(valueSeg);
            }
            return result;
        }

        private Dictionary<string, Lazy<object>> ReadDictionaryLazyCore(StreamSegment current)
        {
            UnderlyingStream.Seek(current.ContentStartOffset, SeekOrigin.Begin);
            var indices = new int[current.ContentLength / 4];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = _reader.ReadInt32();
            }
            var result = new Dictionary<string, Lazy<object>>();
            for (int i = 0; i < indices.Length;)
            {
                var keySeg = ReadSegment(indices[i++]);
                var valueSeg = ReadSegment(indices[i++]);
                result[ReadString(keySeg)] = new Lazy<object>(() => ReadValue(valueSeg), false);
            }
            return result;
        }

        #endregion
    }
}
