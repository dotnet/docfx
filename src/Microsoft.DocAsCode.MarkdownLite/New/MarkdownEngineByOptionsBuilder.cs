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
            var brb = ImmutableList<IMarkdownRule>.Empty.ToBuilder();
            var tbrb = ImmutableList<IMarkdownRule>.Empty.ToBuilder();

            IMarkdownRule rule = new MarkdownNewLineBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            rule = new MarkdownCodeBlockRule();
            brb.Add(rule);
            tbrb.Add(rule);

            if (Options.Gfm)
            {
                rule = new GfmFencesBlockRule();
                brb.Add(rule);
                tbrb.Add(rule);
            }

            if (Options.Gfm)
            {
                rule = new GfmHeadingBlockRule();
                brb.Add(rule);
                tbrb.Add(rule);
            }
            else
            {
                rule = new MarkdownHeadingBlockRule();
                brb.Add(rule);
                tbrb.Add(rule);
            }

            if (Options.Tables)
            {
                rule = new MarkdownNpTableBlockRule();
                tbrb.Add(rule);
            }

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

            if (Options.Tables)
            {
                rule = new MarkdownTableBlockRule();
                tbrb.Add(rule);
            }

            rule = new MarkdownParagraphBlockRule();
            tbrb.Add(rule);

            rule = new MarkdownTextBlockRule();
            brb.Add(rule);

            BlockRules = brb.ToImmutable();
            TopBlockRules = tbrb.ToImmutable();
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
            }
            if (Options.Gfm)
            {
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
