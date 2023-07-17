// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Exceptions;

public class InvalidOverwriteDocumentException : DocfxException
{
    public InvalidOverwriteDocumentException(string message) : base(message)
    {
    }
}
