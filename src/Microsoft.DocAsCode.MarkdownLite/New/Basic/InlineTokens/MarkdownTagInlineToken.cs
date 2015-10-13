namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownTagInlineToken : IMarkdownToken
    {
        public MarkdownTagInlineToken(IMarkdownRule rule, string content)
        {
            Rule = rule;
            Content = content;
        }

        public IMarkdownRule Rule { get; }

        public string Content { get; }
    }
}
