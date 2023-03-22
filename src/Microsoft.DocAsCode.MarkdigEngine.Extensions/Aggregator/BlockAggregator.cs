// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Syntax;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

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
