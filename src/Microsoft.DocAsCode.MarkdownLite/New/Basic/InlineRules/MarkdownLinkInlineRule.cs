namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownLinkInlineRule : MarkdownLinkBaseInlineRule
    {
        public override string Name => "Inline.Link";

        public virtual Regex Link => Regexes.Inline.Link;

        public override IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Link.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return GenerateToken(match.Groups[2].Value, match.Groups[3].Value, match.Groups[1].Value, match.Value[0] == '!');
        }
    }
}
