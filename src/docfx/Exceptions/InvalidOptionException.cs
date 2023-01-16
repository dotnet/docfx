// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;

    public class InvalidOptionException : ArgumentException
    {
        public InvalidOptionException(string message) : base(message)
        {
        }
    }
}
