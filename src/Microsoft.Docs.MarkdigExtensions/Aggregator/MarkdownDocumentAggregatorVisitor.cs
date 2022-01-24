// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

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
