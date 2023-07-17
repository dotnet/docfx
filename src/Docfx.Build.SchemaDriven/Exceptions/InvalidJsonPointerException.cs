// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Exceptions;

public class InvalidJsonPointerException : DocumentException
{
    public InvalidJsonPointerException() : this("The value of json pointer is not valid")
    {
    }

    public InvalidJsonPointerException(string message) : base(message)
    {
    }

    public InvalidJsonPointerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
