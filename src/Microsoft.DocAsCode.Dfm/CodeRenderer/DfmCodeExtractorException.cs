// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class DfmCodeExtractorException : Exception
    {
        public DfmCodeExtractorException()
        {
        }

        public DfmCodeExtractorException(string message) : base(message)
        {
        }

        public DfmCodeExtractorException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DfmCodeExtractorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
