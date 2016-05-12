// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    using Microsoft.DocAsCode.Exceptions;

    [Serializable]
    public class InvalidPreprocessorException : DocfxException
    {

        public InvalidPreprocessorException() : this("Error happens when executing preprocessor")
        {
        }

        public InvalidPreprocessorException(string message) : base(message)
        {
        }

        public InvalidPreprocessorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidPreprocessorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
