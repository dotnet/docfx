// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Git
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class GitException : Exception
    {
        public GitException() : this("Error happens when running git command")
        {
        }

        public GitException(string message):base(message)
        {
        }

        public GitException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GitException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}