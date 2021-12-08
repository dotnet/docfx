// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

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
