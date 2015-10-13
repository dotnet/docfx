namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownParagraphBlockToken : IMarkdownToken
    {
        public MarkdownParagraphBlockToken(IMarkdownRule rule, string content)
        {
            Rule = rule;
            Content = content;
        }

        public IMarkdownRule Rule { get; }

        public string Content { get; }
    }
}
