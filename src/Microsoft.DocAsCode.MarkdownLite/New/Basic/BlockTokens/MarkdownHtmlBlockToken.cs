namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownHtmlBlockToken : IMarkdownToken
    {
        public MarkdownHtmlBlockToken(IMarkdownRule rule, string content, bool pre)
        {
            Rule = rule;
            Content = content;
            Pre = pre;
        }

        public IMarkdownRule Rule { get; }

        public string Content { get; }

        public bool Pre { get; }
    }
}
