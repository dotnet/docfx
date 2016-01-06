// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class DocfxException : Exception
    {
        public DocfxException() : this("Error happens when running docfx")
        {
        }

        public DocfxException(string message) : base(message)
        {
        }

        public DocfxException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DocfxException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
