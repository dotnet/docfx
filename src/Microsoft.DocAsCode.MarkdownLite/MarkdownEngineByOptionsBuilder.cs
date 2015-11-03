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
            var builder = ImmutableList<IMarkdownRule>.Empty.ToBuilder();
            builder.Add(new MarkdownNewLineBlockRule());
            builder.Add(new MarkdownCodeBlockRule());
            if (Options.Gfm)
            {
                builder.Add(new GfmFencesBlockRule());
                builder.Add(new GfmHeadingBlockRule());
            }
            else
            {
                builder.Add(new MarkdownHeadingBlockRule());
            }
            if (Options.Tables)
            {
                builder.Add(new MarkdownNpTableBlockRule());
            }
            builder.Add(new MarkdownLHeadingBlockRule());
            builder.Add(new MarkdownHrBlockRule());
            builder.Add(new MarkdownBlockquoteBlockRule());
            builder.Add(new MarkdownListBlockRule());
            builder.Add(new MarkdownHtmlBlockRule());
            builder.Add(new MarkdownDefBlockRule());
            if (Options.Tables)
            {
                builder.Add(new MarkdownTableBlockRule());
            }
            if (Options.Gfm)
            {
                builder.Add(new GfmParagraphBlockRule());
            }
            else
            {
                builder.Add(new MarkdownParagraphBlockRule());
            }
            builder.Add(new MarkdownTextBlockRule());
            BlockRules = builder.ToImmutable();
        }

        private void BuildInlineRulesByOptions()
        {
            var irb = ImmutableList<IMarkdownRule>.Empty.ToBuilder();
            if (Options.Gfm)
            {
                irb.Add(new GfmEscapeInlineRule());
            }
            else
            {
                irb.Add(new MarkdownEscapeInlineRule());
            }
            irb.Add(new MarkdownAutoLinkInlineRule());
            if (Options.Gfm)
            {
                irb.Add(new GfmUrlInlineRule());
            }
            irb.Add(new MarkdownTagInlineRule());
            irb.Add(new MarkdownLinkInlineRule());
            irb.Add(new MarkdownRefLinkInlineRule());
            irb.Add(new MarkdownNoLinkInlineRule());
            irb.Add(new MarkdownStrongInlineRule());
            irb.Add(new MarkdownEmInlineRule());
            irb.Add(new MarkdownCodeInlineRule());
            irb.Add(new MarkdownBrInlineRule());
            if (Options.Gfm)
            {
                irb.Add(new GfmDelInlineRule());
                irb.Add(new GfmTextInlineRule());
            }
            else
            {
                irb.Add(new MarkdownTextInlineRule());
            }

            InlineRules = irb.ToImmutable();
        }
    }
}
