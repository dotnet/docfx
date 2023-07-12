// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public abstract class BlockAggregator<TBlock> : IBlockAggregator
    where TBlock : class, IBlock
{
    public bool Aggregate(BlockAggregateContext context)
    {
        if (context.CurrentBlock is TBlock block)
        {
            return AggregateCore(block, context);
        }

        return false;
    }

    protected abstract bool AggregateCore(TBlock block, BlockAggregateContext context);
}
