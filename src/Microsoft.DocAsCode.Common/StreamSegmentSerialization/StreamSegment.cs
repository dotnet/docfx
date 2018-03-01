// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.StreamSegmentSerialization
{
    using System.IO;

    public sealed class StreamSegment
    {
        public StreamSegment(Stream underlyingStream, int startOffset, int length, int next, StreamSegmentType contentType)
        {
            UnderlyingStream = underlyingStream;
            StartOffset = startOffset;
            Length = length;
            Next = next;
            ContentType = contentType;
        }

        public Stream UnderlyingStream { get; }
        public int StartOffset { get; }
        public int Length { get; }
        public int Next { get; set; }
        public StreamSegmentType ContentType { get; }
        public int ContentStartOffset => StartOffset + 4 + 4 + 1;
        public int ContentLength => Length - 4 - 4 - 1;
    }
}
