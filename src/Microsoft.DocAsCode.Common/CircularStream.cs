// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;
    using System.Threading;

    public class CircularStream
    {
        private readonly CircularBuffer<byte> _buffer = new CircularBuffer<byte>(4096);
        private readonly object _syncRoot = new object();
        private bool _eof;

        private int Read(byte[] buffer, int offset, int count)
        {
            lock (_syncRoot)
            {
                while (true)
                {
                    var result = _buffer.Read(buffer, offset, count);
                    if (result == 0 && !_eof)
                    {
                        Monitor.Wait(_syncRoot);
                    }
                    else
                    {
                        return result;
                    }
                }
            }
        }

        private void Write(byte[] buffer, int offset, int count)
        {
            lock (_syncRoot)
            {
                if (_buffer.Count == 0)
                {
                    Monitor.PulseAll(_syncRoot);
                }
                _buffer.Write(buffer, offset, count);
            }
        }

        private void Eof()
        {
            lock (_syncRoot)
            {
                _eof = true;
                Monitor.PulseAll(_syncRoot);
            }
        }

        public Stream CreateReaderView()
        {
            return new CircularStreamView(this, false);
        }

        public Stream CreateWriterView()
        {
            return new CircularStreamView(this, true);
        }

        private sealed class CircularStreamView : Stream
        {
            private readonly CircularStream _circularStream;
            private readonly bool _writeMode;

            public CircularStreamView(CircularStream circularStream, bool writeMode)
            {
                _circularStream = circularStream;
                _writeMode = writeMode;
            }

            public override bool CanRead => !_writeMode;

            public override bool CanSeek => false;

            public override bool CanWrite => _writeMode;

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
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_writeMode)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    return _circularStream.Read(buffer, offset, count);
                }
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
                if (_writeMode)
                {
                    _circularStream.Write(buffer, offset, count);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            public override void Close()
            {
                if (_writeMode)
                {
                    _circularStream.Eof();
                }
                base.Close();
            }
        }
    }
}
