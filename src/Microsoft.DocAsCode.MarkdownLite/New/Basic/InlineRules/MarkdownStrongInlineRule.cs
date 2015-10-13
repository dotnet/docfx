namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownStrongInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Strong";

        public virtual Regex Strong => Regexes.Inline.Strong;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Strong.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownStrongInlineToken(this, match.NotEmpty(2, 1));
        }
    }
}
