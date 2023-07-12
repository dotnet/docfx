// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Parsers;
using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class MonikerRangeBlock : ContainerBlock
{
    public string MonikerRange { get; set; }
    public int ColonCount { get; set; }
    public bool Closed { get; set; }
    public MonikerRangeBlock(BlockParser parser) : base(parser)
    {
    }
}
