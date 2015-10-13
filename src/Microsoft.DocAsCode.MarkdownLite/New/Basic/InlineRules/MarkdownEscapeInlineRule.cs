namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownEscapeInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Escape";

        public virtual Regex Escape => Regexes.Inline.Escape;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Escape.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new MarkdownEscapeInlineToken(this, match.Groups[1].Value);
        }
    }
}
