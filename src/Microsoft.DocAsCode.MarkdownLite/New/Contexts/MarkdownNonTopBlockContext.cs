namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    internal sealed class MarkdownNonTopBlockContext : MarkdownBlockContext
    {
        internal MarkdownNonTopBlockContext(ImmutableList<IMarkdownRule> rules, IMarkdownContext inlineContext)
            : base(inlineContext)
        {
            Rules = rules;
        }

        internal ImmutableList<IMarkdownRule> Rules { get; }

        public override ImmutableList<IMarkdownRule> GetRules()
        {
            return Rules;
        }

        public override IMarkdownContext GetNonTopBlockContext()
        {
            return this;
        }
    }
}
