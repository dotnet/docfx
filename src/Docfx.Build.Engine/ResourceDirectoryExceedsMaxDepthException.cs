// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Exceptions;

namespace Docfx.Build.Engine;

public class ResourceFileExceedsMaxDepthException : DocfxException
{
    public int MaxDepth { get; }
    public string DirectoryName { get; }
    public string ResourceName { get; }

    public ResourceFileExceedsMaxDepthException(int maxDepth, string fileName, string resourceName) : base($"Resource file \"{fileName}\" in resource \"{resourceName}\" exceeds the max allowed depth {maxDepth}.")
    {
        MaxDepth = maxDepth;
        DirectoryName = fileName;
        ResourceName = resourceName;
    }
}
