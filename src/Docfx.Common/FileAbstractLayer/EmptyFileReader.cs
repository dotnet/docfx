// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

internal sealed class EmptyFileReader : IFileReader
{
    public static readonly EmptyFileReader Instance = new();

    private EmptyFileReader()
    {
    }

    public PathMapping? FindFile(RelativePath file) => null;

    public IEnumerable<RelativePath> EnumerateFiles() => [];

    public string GetExpectedPhysicalPath(RelativePath file) => null;
}
