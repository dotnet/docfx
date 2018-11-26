// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class InvalidOverwriteDocumentException : DocfxException
    {
        public InvalidOverwriteDocumentException() : base()
        {
        }

        public InvalidOverwriteDocumentException(string message) : base(message)
        {
        }

        public InvalidOverwriteDocumentException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidOverwriteDocumentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
