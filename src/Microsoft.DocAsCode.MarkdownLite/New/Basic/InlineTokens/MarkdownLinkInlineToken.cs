namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownLinkInlineToken : IMarkdownToken
    {
        public MarkdownLinkInlineToken(IMarkdownRule rule, string href, string title, string text, bool shouldApplyInlineRule = false)
        {
            Rule = rule;
            Href = href;
            Title = title;
            Text = text;
            ShouldApplyInlineRule = shouldApplyInlineRule;
        }

        public IMarkdownRule Rule { get; }

        public string Href { get; }

        public string Title { get; }

        public string Text { get; }

        public bool ShouldApplyInlineRule { get; set; }
    }
}
