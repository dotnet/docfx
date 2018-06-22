// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Throw this exception in case of known error that should stop execution of the program.
    /// E.g, loading a malformed configuration.
    /// </summary>
    internal class DocfxException : Exception
    {
        public Error Error { get; }

        public DocfxException(Error error, Exception innerException = null)
            : base(error.Message, innerException)
        {
            Error = error;
        }
    }
}
