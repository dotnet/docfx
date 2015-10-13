namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownLooseItemBlockToken : IMarkdownToken
    {
        public MarkdownLooseItemBlockToken(IMarkdownRule rule, ImmutableArray<IMarkdownToken> tokens)
        {
            Rule = rule;
            Tokens = tokens;
        }

        public IMarkdownRule Rule { get; }

        public ImmutableArray<IMarkdownToken> Tokens { get; }
    }
}
