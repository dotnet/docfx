namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownLHeadingBlockRule : IMarkdownRule
    {
        public string Name => "LHeading";

        public virtual Regex LHeading => Regexes.Block.LHeading;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = LHeading.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new MarkdownHeadingBlockToken(this, match.Groups[1].Value, match.Groups[2].Value == "=" ? 1 : 2);
        }
    }
}
