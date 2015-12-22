// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    public class InvalidOptionException : ArgumentException
    {
        public InvalidOptionException() : this("Invalid option!")
        {
        }

        public InvalidOptionException(string message) : base(message)
        {
        }

        public InvalidOptionException(string message, string paramName) : base(message, paramName)
        {
        }

        public InvalidOptionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public InvalidOptionException(string message, string paramName, Exception innerException) : base(message, paramName, innerException)
        {
        }

        protected InvalidOptionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
