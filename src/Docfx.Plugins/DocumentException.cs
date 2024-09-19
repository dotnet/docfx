// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public class DocumentException : Exception
{
    public DocumentException() { }
    public DocumentException(string message) : base(message) { }
    public DocumentException(string message, Exception inner) : base(message, inner) { }
}
