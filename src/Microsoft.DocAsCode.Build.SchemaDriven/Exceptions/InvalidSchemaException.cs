// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Exceptions;

public class InvalidSchemaException : DocumentException
{
    public InvalidSchemaException(string message) : base(message)
    {
    }

    public InvalidSchemaException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
