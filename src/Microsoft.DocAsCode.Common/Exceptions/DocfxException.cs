// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class DocfxException : Exception
    {
        private const string DefaultMessage = "Error happens when running docfx";

        public DocfxException() : this(DefaultMessage)
        {
        }

        public DocfxException(string message) : base(message ?? DefaultMessage)
        {
        }

        public DocfxException(string message, Exception innerException) : base(message ?? DefaultMessage, innerException)
        {
        }

        protected DocfxException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
