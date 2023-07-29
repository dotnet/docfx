// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.MarkdigEngine.Extensions;
using Markdig.Syntax;

namespace Docfx.MarkdigEngine;

public class TabGroupIdRewriter : IMarkdownObjectRewriter
{
    private int _id;

    public void PostProcess(IMarkdownObject markdownObject)
    {
    }

    public void PreProcess(IMarkdownObject markdownObject)
    {
    }

    public IMarkdownObject Rewrite(IMarkdownObject markdownObject)
    {
        if (markdownObject is TabGroupBlock block)
        {
            block.Id = ++_id;
        }

        return markdownObject;
    }
}
