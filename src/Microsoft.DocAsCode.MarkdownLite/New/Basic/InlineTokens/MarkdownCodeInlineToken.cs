namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownCodeInlineToken : IMarkdownToken
    {
        public MarkdownCodeInlineToken(IMarkdownRule rule, string content)
        {
            Rule = rule;
            Content = content;
        }

        public IMarkdownRule Rule { get; }

        public string Content { get; }
    }
}
