// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    using Microsoft.DocAsCode.Plugins;

    [Serializable]
    public class InvalidSchemaException : DocumentException
    {
        public InvalidSchemaException() : this("Document schema is not valid")
        {
        }

        public InvalidSchemaException(string message) : base(message)
        {
        }

        public InvalidSchemaException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidSchemaException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
