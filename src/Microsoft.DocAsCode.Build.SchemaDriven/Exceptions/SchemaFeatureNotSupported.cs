// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class SchemaFeatureNotSupported : DocfxException
    {
        public SchemaFeatureNotSupported() : this("This feature is not supported in current schema driven document processor")
        {
        }

        public SchemaFeatureNotSupported(string message) : base(message)
        {
        }

        public SchemaFeatureNotSupported(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SchemaFeatureNotSupported(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
