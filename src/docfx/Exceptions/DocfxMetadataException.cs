// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    public class DocfxMetadataException : Exception
    {
        public DocfxMetadataException() : this("Error happens when running docfx metadata")
        {
        }

        public DocfxMetadataException(string message) : base(message)
        {
        }

        public DocfxMetadataException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DocfxMetadataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
