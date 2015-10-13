namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownBrInlineToken : IMarkdownToken
    {
        public MarkdownBrInlineToken(IMarkdownRule rule)
        {
            Rule = rule;
        }

        public IMarkdownRule Rule { get; }
    }
}
