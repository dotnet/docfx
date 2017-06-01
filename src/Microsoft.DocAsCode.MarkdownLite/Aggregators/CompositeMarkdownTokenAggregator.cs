// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class CompositeMarkdownTokenAggregator : IMarkdownTokenAggregator
    {
        private readonly ImmutableArray<IMarkdownTokenAggregator> _aggregators;

        public CompositeMarkdownTokenAggregator(IEnumerable<IMarkdownTokenAggregator> aggregators)
        {
            _aggregators = aggregators.ToImmutableArray();
        }

        public bool Aggregate(IMarkdownTokenAggregateContext context)
        {
            foreach (var agg in _aggregators)
            {
                if (agg.Aggregate(context))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
