// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class TabContentBlock : ContainerBlock
{
    public TabContentBlock(List<Block> blocks)
        : base(null)
    {
        foreach (var item in blocks)
        {
            Add(item);
        }
    }
}
