namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownImageInlineToken : IMarkdownToken
    {
        public MarkdownImageInlineToken(IMarkdownRule rule, string href, string title, string text)
        {
            Rule = rule;
            Href = href;
            Title = title;
            Text = text;
        }

        public IMarkdownRule Rule { get; }

        public string Href { get; }

        public string Title { get; }

        public string Text { get; }
    }
}
