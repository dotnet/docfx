// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public abstract class MarkdownTokenAggregator<THeader> : IMarkdownTokenAggregator
        where THeader : IMarkdownToken
    {
        public bool Aggregate(IMarkdownTokenAggregateContext context)
        {
            if (context.CurrentToken is THeader token)
            {
                return AggregateCore(token, context);
            }
            return false;
        }

        protected abstract bool AggregateCore(THeader headToken, IMarkdownTokenAggregateContext context);
    }
}
