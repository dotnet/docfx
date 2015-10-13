namespace Microsoft.DocAsCode.MarkdownLite
{
    public sealed class MarkdownIgnoreToken : IMarkdownToken
    {
        public MarkdownIgnoreToken(IMarkdownRule rule)
        {
            Rule = rule;
        }

        public IMarkdownRule Rule { get; }
    }
}
