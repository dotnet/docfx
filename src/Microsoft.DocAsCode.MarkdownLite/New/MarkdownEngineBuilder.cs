namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownEngineBuilder
    {
        public MarkdownEngineBuilder(Options options)
        {
            Options = options;
        }

        public Options Options { get; }

        public ImmutableList<IMarkdownRule> BlockRules { get; set; } = ImmutableList<IMarkdownRule>.Empty;

        public ImmutableList<IMarkdownRule> TopBlockRules { get; set; } = ImmutableList<IMarkdownRule>.Empty;

        public ImmutableList<IMarkdownRule> InlineRules { get; set; } = ImmutableList<IMarkdownRule>.Empty;

        protected virtual IMarkdownContext CreateParseContext()
        {
            var inline = new MarkdownInlineContext(InlineRules);
            var block = new MarkdownNonTopBlockContext(BlockRules, inline);
            var topBlock = new MarkdownTopBlockContext(TopBlockRules, block, inline);
            return topBlock;
        }

        public virtual MarkdownEngine CreateEngine(object renderer)
        {
            return new MarkdownEngine(CreateParseContext(), renderer, Options);
        }
    }
}
