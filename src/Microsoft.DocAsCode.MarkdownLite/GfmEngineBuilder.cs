// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class GfmEngineBuilder : MarkdownEngineBuilder
    {
        public GfmEngineBuilder(Options options)
            : base(options)
        {
            BuildRules();
        }

        protected virtual void BuildRules()
        {
            BuildBlockRules();
            BuildInlineRules();
        }

        protected virtual void BuildBlockRules()
        {
            var builder = ImmutableList.CreateBuilder<IMarkdownRule>();
            builder.Add(new MarkdownNewLineBlockRule());
            builder.Add(new MarkdownCodeBlockRule());
            builder.Add(new GfmFencesBlockRule());
            builder.Add(new MarkdownHeadingBlockRule());
            builder.Add(new MarkdownNpTableBlockRule());
            builder.Add(new MarkdownLHeadingBlockRule());
            builder.Add(new MarkdownHrBlockRule());
            builder.Add(new MarkdownBlockquoteBlockRule());
            builder.Add(new MarkdownListBlockRule());
            builder.Add(new GfmHtmlCommentBlockRule());
            builder.Add(new MarkdownPreElementBlockRule());
            builder.Add(new MarkdownHtmlBlockRule());
            builder.Add(new MarkdownDefBlockRule());
            builder.Add(new MarkdownTableBlockRule());
            builder.Add(new MarkdownTextBlockRule());
            BlockRules = builder.ToImmutable();
        }

        protected virtual void BuildInlineRules()
        {
            var builder = ImmutableList.CreateBuilder<IMarkdownRule>();
            builder.Add(new GfmEscapeInlineRule());
            builder.Add(new MarkdownCommentInlineRule());
            builder.Add(new MarkdownAutoLinkInlineRule());
            builder.Add(new GfmUrlInlineRule());
            builder.Add(new MarkdownPreElementInlineRule());
            builder.Add(new MarkdownTagInlineRule());
            builder.Add(new MarkdownLinkInlineRule());
            builder.Add(new MarkdownRefLinkInlineRule());
            builder.Add(new MarkdownNoLinkInlineRule());
            builder.Add(new GfmStrongEmInlineRule());
            builder.Add(new GfmStrongInlineRule());
            builder.Add(new GfmEmInlineRule());
            builder.Add(new MarkdownCodeInlineRule());
            builder.Add(new MarkdownBrInlineRule());
            builder.Add(new GfmDelInlineRule());
            builder.Add(new MarkdownEscapedTextInlineRule());
            builder.Add(new GfmEmojiInlineRule());
            builder.Add(new GfmTextInlineRule());
            InlineRules = builder.ToImmutable();
        }

    }
}
