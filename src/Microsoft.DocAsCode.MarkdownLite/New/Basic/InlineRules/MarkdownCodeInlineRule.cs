namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownCodeInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Code";

        public virtual Regex Code => Regexes.Inline.Code;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Code.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownCodeInlineToken(this, match.Groups[2].Value);
        }
    }
}
