// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

public class ResourceInfo
{
    public string Path { get; }
    public string Content { get; }
    public ResourceInfo(string path, string content)
    {
        Path = path;
        Content = content;
    }
}
