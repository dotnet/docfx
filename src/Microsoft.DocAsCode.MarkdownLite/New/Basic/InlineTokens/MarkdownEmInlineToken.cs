namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownEmInlineToken : IMarkdownToken
    {
        public MarkdownEmInlineToken(IMarkdownRule rule, string content)
        {
            Rule = rule;
            Content = content;
        }

        public IMarkdownRule Rule { get; }

        public string Content { get; }
    }
}
