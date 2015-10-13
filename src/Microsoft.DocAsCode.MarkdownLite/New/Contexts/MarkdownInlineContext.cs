namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownInlineContext : IMarkdownContext
    {
        public MarkdownInlineContext(ImmutableList<IMarkdownRule> rules)
        {
            Rules = rules;
        }

        internal ImmutableList<IMarkdownRule> Rules { get; }

        public ImmutableList<IMarkdownRule> GetRules() => Rules;

        public bool InLink { get; set; }
    }
}
