namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownCodeBlockToken : IMarkdownToken
    {
        public MarkdownCodeBlockToken(IMarkdownRule rule, string code, string lang = null)
        {
            Rule = rule;
            Code = code;
            Lang = lang;
        }

        public IMarkdownRule Rule { get; }

        public string Code { get; }

        public string Lang { get; }
    }
}
