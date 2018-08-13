// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Throw this exception in case of known error that should be catched in outer scope.
    /// E.g, GitHub rate limit exceeded, which should be catched an wrapped in more user friendly warning message.
    /// </summary>
    internal class DocfxInternalException : Exception
    {
        public DocfxInternalException(string message)
            : base(message)
        {
        }
    }
}
