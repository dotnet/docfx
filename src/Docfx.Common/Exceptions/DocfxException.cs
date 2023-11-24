// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Exceptions;

public class DocfxException : Exception
{
    private const string DefaultMessage = "Error happens when running docfx";

    public DocfxException() : this(DefaultMessage)
    {
    }

    public DocfxException(string message) : base(message ?? DefaultMessage)
    {
    }

    public DocfxException(string message, Exception innerException) : base(message ?? DefaultMessage, innerException)
    {
    }
}
