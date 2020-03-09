// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal class MonikerRangeException : Exception
    {
        public MonikerRangeException(string message)
            : base(message)
        {
        }
    }
}
