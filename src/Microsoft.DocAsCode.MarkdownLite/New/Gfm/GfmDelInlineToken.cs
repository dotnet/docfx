namespace Microsoft.DocAsCode.MarkdownLite
{
    public class GfmDelInlineToken : IMarkdownToken
    {
        public GfmDelInlineToken(IMarkdownRule rule, string content)
        {
            Rule = rule;
            Content = content;
        }

        public IMarkdownRule Rule { get; }

        public string Content { get; }
    }
}
