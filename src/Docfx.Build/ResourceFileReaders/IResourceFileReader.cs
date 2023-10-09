// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

public interface IResourceFileReader
{
    IEnumerable<string> Names { get; }

    string GetResource(string name);

    IEnumerable<ResourceInfo> GetResources(string selector);

    IEnumerable<KeyValuePair<string, Stream>> GetResourceStreams(string selector);

    Stream GetResourceStream(string name);
}
