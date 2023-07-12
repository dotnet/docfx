// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.OverwriteDocuments;

public class MarkdownFragmentsException : Exception
{
    public int Position { get; } = -1;

    public MarkdownFragmentsException(string message) : base(message)
    {
    }

    public MarkdownFragmentsException(string message, int position) : base(message)
    {
        Position = position;
    }

    public MarkdownFragmentsException(string message, int position, Exception inner) : base(message, inner)
    {
        Position = position;
    }
}
