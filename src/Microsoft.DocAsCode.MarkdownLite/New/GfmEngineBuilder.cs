namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class GfmEngineBuilder : MarkdownEngineBuilder
    {
        public GfmEngineBuilder(Options options)
            : base(options)
        {
            BuildRules();
        }

        protected void BuildRules()
        {
            BuildBlockRules();
            BuildInlineRules();
        }

        protected void BuildBlockRules()
        {
            var brb = ImmutableList<IMarkdownRule>.Empty.ToBuilder();
            var tbrb = ImmutableList<IMarkdownRule>.Empty.ToBuilder();

            IMarkdownRule rule = new MarkdownNewLineBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new MarkdownNewLineBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new MarkdownCodeBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new GfmFencesBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new GfmHeadingBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new MarkdownNpTableBlockRule();
            tbrb.Add(rule);

            rule = new MarkdownLHeadingBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new MarkdownHrBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new MarkdownBlockquoteBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new MarkdownListBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new MarkdownHtmlBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new MarkdownDefBlockRule();
            tbrb.Add(rule);

            rule = new MarkdownTableBlockRule();
            tbrb.Add(rule);

            rule = new MarkdownParagraphBlockRule();
            tbrb.Add(rule);

            rule = new MarkdownTextBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            BlockRules = brb.ToImmutable();
            TopBlockRules = tbrb.ToImmutable();
        }

        protected void BuildInlineRules()
        {
            var builder = ImmutableList<IMarkdownRule>.Empty.ToBuilder();

            builder.Add(new GfmEscapeInlineRule());
            builder.Add(new MarkdownAutoLinkInlineRule());
            builder.Add(new GfmUrlInlineRule());
            builder.Add(new MarkdownTagInlineRule());
            builder.Add(new MarkdownLinkInlineRule());
            builder.Add(new MarkdownRefLinkInlineRule());
            builder.Add(new MarkdownNoLinkInlineRule());
            builder.Add(new MarkdownStrongInlineRule());
            builder.Add(new MarkdownEmInlineRule());
            builder.Add(new MarkdownCodeInlineRule());
            builder.Add(new MarkdownBrInlineRule());
            builder.Add(new GfmDelInlineRule());
            builder.Add(new GfmTextInlineRule());

            InlineRules = builder.ToImmutable();
        }

    }
}
