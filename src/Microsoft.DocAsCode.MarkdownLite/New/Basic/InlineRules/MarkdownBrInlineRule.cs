namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownBrInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Br";

        public virtual Regex Br => Regexes.Inline.Br;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Br.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownBrInlineToken(this);
        }
    }
}
