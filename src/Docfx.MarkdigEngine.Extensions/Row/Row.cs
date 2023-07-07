// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Parsers;
using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class RowBlock : ContainerBlock
{
    public int ColonCount { get; set; }
    public RowBlock(BlockParser parser) : base(parser)
    {
    }
}
