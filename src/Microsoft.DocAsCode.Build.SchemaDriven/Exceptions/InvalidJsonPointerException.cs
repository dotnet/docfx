// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    using Microsoft.DocAsCode.Plugins;

    [Serializable]
    public class InvalidJsonPointerException : DocumentException
    {
        public InvalidJsonPointerException() : this("The value of json pointer is not valid")
        {
        }

        public InvalidJsonPointerException(string message) : base(message)
        {
        }

        public InvalidJsonPointerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidJsonPointerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
