// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Exceptions;

internal class ExtractMetadataException : DocfxException
{
    public ExtractMetadataException(string message) : base(message)
    {
    }

    public ExtractMetadataException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
