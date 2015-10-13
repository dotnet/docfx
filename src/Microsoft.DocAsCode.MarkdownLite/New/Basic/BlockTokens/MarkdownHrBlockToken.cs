namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownHrBlockToken : IMarkdownToken
    {
        public MarkdownHrBlockToken(IMarkdownRule rule)
        {
            Rule = rule;
        }

        public IMarkdownRule Rule { get; }
    }
}
