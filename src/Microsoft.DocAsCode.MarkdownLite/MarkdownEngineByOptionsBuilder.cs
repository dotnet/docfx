// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownEngineByOptionsBuilder
        : MarkdownEngineBuilder
    {
        public MarkdownEngineByOptionsBuilder(Options options)
            : base(options)
        {
            BuildRulesByOptions();
        }

        private void BuildRulesByOptions()
        {
            BuildBlockRulesByOptions();
            BuildInlineRulesByOptions();
        }

        private void BuildBlockRulesByOptions()
        {
            var builder = ImmutableList.CreateBuilder<IMarkdownRule>();
            builder.Add(new MarkdownNewLineBlockRule());
            builder.Add(new MarkdownCodeBlockRule());
            if (Options.Gfm)
            {
                builder.Add(new GfmFencesBlockRule());
            }
            builder.Add(new MarkdownHeadingBlockRule());
            if (Options.Tables)
            {
                builder.Add(new MarkdownNpTableBlockRule());
            }
            builder.Add(new MarkdownLHeadingBlockRule());
            builder.Add(new MarkdownHrBlockRule());
            builder.Add(new MarkdownBlockquoteBlockRule());
            builder.Add(new MarkdownListBlockRule());
            if (Options.Gfm)
            {
                builder.Add(new GfmHtmlCommentBlockRule());
            }
            builder.Add(new MarkdownPreElementBlockRule());
            builder.Add(new MarkdownHtmlBlockRule());
            builder.Add(new MarkdownDefBlockRule());
            if (Options.Tables)
            {
                builder.Add(new MarkdownTableBlockRule());
            }
            builder.Add(new MarkdownTextBlockRule());
            BlockRules = builder.ToImmutable();
        }

        private void BuildInlineRulesByOptions()
        {
            var builder = ImmutableList.CreateBuilder<IMarkdownRule>();
            if (Options.Gfm)
            {
                builder.Add(new GfmEscapeInlineRule());
            }
            else
            {
                builder.Add(new MarkdownEscapeInlineRule());
            }
            builder.Add(new MarkdownAutoLinkInlineRule());
            if (Options.Gfm)
            {
                builder.Add(new GfmUrlInlineRule());
            }
            builder.Add(new MarkdownPreElementInlineRule());
            builder.Add(new MarkdownTagInlineRule());
            builder.Add(new MarkdownLinkInlineRule());
            builder.Add(new MarkdownRefLinkInlineRule());
            builder.Add(new MarkdownNoLinkInlineRule());
            if (Options.Gfm)
            {
                builder.Add(new GfmStrongEmInlineRule());
                builder.Add(new GfmStrongInlineRule());
                builder.Add(new GfmEmInlineRule());
            }
            else
            {
                builder.Add(new MarkdownStrongInlineRule());
                builder.Add(new MarkdownEmInlineRule());
            }
            builder.Add(new MarkdownCodeInlineRule());
            builder.Add(new MarkdownBrInlineRule());
            if (Options.Gfm)
            {
                builder.Add(new GfmDelInlineRule());
                builder.Add(new MarkdownEscapedTextInlineRule());
                builder.Add(new GfmEmojiInlineRule());
                builder.Add(new GfmTextInlineRule());
            }
            else
            {
                builder.Add(new MarkdownEscapedTextInlineRule());
                builder.Add(new MarkdownTextInlineRule());
            }

            InlineRules = builder.ToImmutable();
        }
    }
}
