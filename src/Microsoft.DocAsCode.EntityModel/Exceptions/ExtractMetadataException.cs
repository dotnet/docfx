// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ExtractMetadataException : DocfxException
    {
        public ExtractMetadataException() : this("Error happens when extracting metadata")
        {
        }

        public ExtractMetadataException(string message) : base(message)
        {
        }

        public ExtractMetadataException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExtractMetadataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
