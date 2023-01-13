// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Exceptions
{
    using System;

    using Microsoft.DocAsCode.Plugins;

    public class InvalidSchemaException : DocumentException
    {
        public InvalidSchemaException(string message) : base(message)
        {
        }

        public InvalidSchemaException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
