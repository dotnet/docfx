// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmTabGroupBlockToken : IMarkdownExpression, IMarkdownRewritable<DfmTabGroupBlockToken>
    {
        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Id { get; }

        public ImmutableArray<DfmTabItemBlockToken> Items { get; }

        public int ActiveTabIndex { get; }

        public SourceInfo SourceInfo { get; }

        public DfmTabGroupBlockToken(IMarkdownRule rule, IMarkdownContext context, string id, ImmutableArray<DfmTabItemBlockToken> items, int activeTabIndex, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Id = id;
            Items = items;
            ActiveTabIndex = activeTabIndex;
            SourceInfo = sourceInfo;
        }

        public IEnumerable<IMarkdownToken> GetChildren()
        {
            return Items;
        }

        public DfmTabGroupBlockToken Rewrite(IMarkdownRewriteEngine rewriteEngine)
        {
            var items = Items;
            for (int i = 0; i < Items.Length; i++)
            {
                var item = Items[i].Rewrite(rewriteEngine);
                if (item != null && item != Items[i])
                {
                    items = items.SetItem(i, item);
                }
            }
            if (items == Items)
            {
                return this;
            }
            return new DfmTabGroupBlockToken(Rule, Context, Id, items, ActiveTabIndex, SourceInfo);
        }
    }
}
