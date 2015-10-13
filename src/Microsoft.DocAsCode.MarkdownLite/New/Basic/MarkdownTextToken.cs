namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownTextToken : IMarkdownToken
    {
        public MarkdownTextToken(IMarkdownRule rule, string content)
        {
            Rule = rule;
            Content = content;
        }

        public IMarkdownRule Rule { get; }

        public string Content { get; }
    }
}
