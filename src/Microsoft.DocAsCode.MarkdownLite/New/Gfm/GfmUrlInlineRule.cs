namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class GfmUrlInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Gfm.Url";

        public virtual Regex Url => Regexes.Inline.Gfm.Url;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Url.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            var text = StringHelper.Escape(match.Groups[1].Value);
            return new MarkdownLinkInlineToken(this, text, null, text);
        }
    }
}
