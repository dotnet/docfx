namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownEscapeInlineToken : IMarkdownToken
    {
        public MarkdownEscapeInlineToken(IMarkdownRule rule, string content)
        {
            Rule = rule;
            Content = content;
        }

        public IMarkdownRule Rule { get; }

        public string Content { get; }
    }
}
