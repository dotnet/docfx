// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class MarkdownDocumentAggregatorVisitor
{
    private readonly IBlockAggregator _aggregator;

    public MarkdownDocumentAggregatorVisitor(IBlockAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    public void Visit(MarkdownDocument document)
    {
        if (_aggregator == null)
        {
            return;
        }

        VisitContainerBlock(document);
    }

    private void VisitContainerBlock(ContainerBlock blocks)
    {
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block is ContainerBlock containerBlock)
            {
                VisitContainerBlock(containerBlock);
            }

            var context = new BlockAggregateContext(blocks);
            Aggregate(context);
        }
    }

    private void Aggregate(BlockAggregateContext context)
    {
        while (context.NextBlock())
        {
            _aggregator.Aggregate(context);
        }
    }
}
