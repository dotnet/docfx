// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public readonly struct LinkSourceInfo
{
    public string Target { get; init; }
    public string Anchor { get; init; }
    public string SourceFile { get; init; }
    public int LineNumber { get; init; }
}
