// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    public class DocfxInitException : Exception
    {
        public DocfxInitException() : this("Error happens when running docfx init")
        {
        }

        public DocfxInitException(string message) : base(message)
        {
        }

        public DocfxInitException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DocfxInitException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
