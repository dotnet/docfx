namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownStrongInlineToken : IMarkdownToken
    {
        public MarkdownStrongInlineToken(IMarkdownRule rule, string content)
        {
            Rule = rule;
            Content = content;
        }

        public IMarkdownRule Rule { get; }

        public string Content { get; }
    }
}
