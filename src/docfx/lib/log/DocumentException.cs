// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Throw a DocumentException in case of an known error that should stop execution of the program.
    /// E.g, loading a malformed configuration.
    /// </summary>
    internal class DocumentException : Exception
    {
        /// <summary>
        /// Gets the error code of this exception.
        /// Error codes should be properly documented in /docs/errors.md
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets the file path of the document that has the error, or null if not applicable.
        /// Relative file path preferred.
        /// </summary>
        public string File { get; }

        public DocumentException(string code, string message, string file = null)
            : base(message)
        {
            Code = code;
            File = file;
        }
    }
}
