// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Plugins;

public struct LinkSourceInfo
{
    public string Target { get; set; }
    public string Anchor { get; set; }
    public string SourceFile { get; set; }
    public int LineNumber { get; set; }
}
