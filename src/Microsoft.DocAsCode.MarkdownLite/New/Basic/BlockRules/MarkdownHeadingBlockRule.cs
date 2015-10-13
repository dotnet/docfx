namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownHeadingBlockRule : IMarkdownRule
    {
        public string Name => "Heading";

        public virtual Regex Heading => Regexes.Block.Heading;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Heading.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new MarkdownHeadingBlockToken(this, match.Groups[2].Value, match.Groups[1].Value.Length);
        }
    }
}
