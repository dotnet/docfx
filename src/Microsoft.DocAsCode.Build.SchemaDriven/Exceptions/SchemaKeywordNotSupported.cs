// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class SchemaKeywordNotSupported : DocfxException
    {
        public SchemaKeywordNotSupported() : this("This")
        {
        }

        public SchemaKeywordNotSupported(string keyword) : base($"{keyword} keyword is not supported in current schema driven document processor")
        {
        }

        public SchemaKeywordNotSupported(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SchemaKeywordNotSupported(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
