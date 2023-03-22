// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments;

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
