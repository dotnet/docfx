// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.DocAsCode.MarkdigEngine;

public class TabGroupIdRewriter : IMarkdownObjectRewriter
{
    private Dictionary<string, int> _dict = new();

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
            var groupId = block.Id;
            while (true)
            {
                if (_dict.TryGetValue(groupId, out int index))
                {
                    _dict[groupId] += 1;
                    groupId = $"{groupId}-{index}";
                    block.Id = groupId;
                    return block;
                }
                else
                {
                    _dict.Add(groupId, 1);
                    return markdownObject;
                }
            }
        }

        return markdownObject;
    }
}
