// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class SchemaFeatureNotSupportedException : DocfxException
    {
        public SchemaFeatureNotSupportedException() : this("This feature is not supported in current schema driven document processor")
        {
        }

        public SchemaFeatureNotSupportedException(string message) : base(message)
        {
        }

        public SchemaFeatureNotSupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SchemaFeatureNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
