// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

public abstract class BlockAggregator<TBlock> : IBlockAggregator
    where TBlock : class, IBlock
{
    public bool Aggregate(BlockAggregateContext context)
    {
        return context.CurrentBlock is TBlock block && AggregateCore(block, context);
    }

    protected abstract bool AggregateCore(TBlock block, BlockAggregateContext context);
}
