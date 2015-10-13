namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    internal sealed class MarkdownTopBlockContext : MarkdownBlockContext
    {
        internal MarkdownTopBlockContext(ImmutableList<IMarkdownRule> rules, MarkdownNonTopBlockContext nonTopContext, IMarkdownContext inlineContext)
            : base(inlineContext)
        {
            Rules = rules;
            NonTopContext = nonTopContext;
        }

        internal ImmutableList<IMarkdownRule> Rules { get; }

        internal MarkdownNonTopBlockContext NonTopContext { get; }

        public override ImmutableList<IMarkdownRule> GetRules()
        {
            return Rules;
        }

        public override IMarkdownContext GetNonTopBlockContext()
        {
            return NonTopContext;
        }
    }
}
