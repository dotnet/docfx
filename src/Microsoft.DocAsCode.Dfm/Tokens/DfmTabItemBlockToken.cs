// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmTabItemBlockToken : IMarkdownExpression, IMarkdownRewritable<DfmTabItemBlockToken>
    {
        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Id { get; }

        public string Condition { get; }

        public DfmTabTitleBlockToken Title { get; }

        public DfmTabContentBlockToken Content { get; }

        public bool Visible { get; }

        public SourceInfo SourceInfo { get; }

        public DfmTabItemBlockToken(IMarkdownRule rule, IMarkdownContext context, string id, string condition, DfmTabTitleBlockToken title, DfmTabContentBlockToken content, bool visible, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Id = id;
            Condition = condition;
            Title = title;
            Content = content;
            Visible = visible;
            SourceInfo = sourceInfo;
        }

        public IEnumerable<IMarkdownToken> GetChildren()
        {
            return new IMarkdownToken[] { Title, Content };
        }

        public DfmTabItemBlockToken Rewrite(IMarkdownRewriteEngine rewriteEngine)
        {
            var title = Title.Rewrite(rewriteEngine);
            var content = Content.Rewrite(rewriteEngine);
            if (title == Title && content == Content)
            {
                return this;
            }
            return new DfmTabItemBlockToken(Rule, Context, Id, Condition, title, content, Visible, SourceInfo);
        }

        public DfmTabItemBlockToken SetVisible(bool visible)
        {
            if (visible == Visible)
            {
                return this;
            }
            return new DfmTabItemBlockToken(Rule, Context, Id, Condition, Title, Content, visible, SourceInfo);
        }
    }
}
