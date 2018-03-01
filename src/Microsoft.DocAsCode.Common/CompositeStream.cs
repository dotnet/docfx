// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Write-only composite stream.
    /// </summary>
    public class CompositeStream : Stream
    {
        private readonly Stream[] _streams;

        public CompositeStream(params Stream[] streams)
            : this((IEnumerable<Stream>)streams)
        {
        }

        public CompositeStream(IEnumerable<Stream> streams)
        {
            if (streams == null)
            {
                throw new ArgumentNullException(nameof(streams));
            }
            foreach (var s in streams)
            {
                if (!s.CanWrite)
                {
                    throw new ArgumentException("Stream should be writable.");
                }
            }
            _streams = streams.ToArray();
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
            foreach (var s in _streams)
            {
                s.Flush();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            foreach (var s in _streams)
            {
                s.Write(buffer, offset, count);
            }
        }

        public override void Close()
        {
            foreach (var s in _streams)
            {
                s.Close();
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(from s in _streams select s.FlushAsync(cancellationToken));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override int ReadByte()
        {
            throw new NotSupportedException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.WhenAll(from s in _streams select s.WriteAsync(buffer, offset, count, cancellationToken));
        }

        public override void WriteByte(byte value)
        {
            foreach (var s in _streams)
            {
                s.WriteByte(value);
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return Task.WhenAll(from s in _streams select s.CopyToAsync(destination, bufferSize, cancellationToken));
        }
    }
}
