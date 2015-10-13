namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownTagInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Tag";

        public virtual Regex Tag => Regexes.Inline.Tag;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Tag.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            var c = (MarkdownInlineContext)engine.Context;
            if (!c.InLink && Regexes.Lexers.StartHtmlLink.IsMatch(match.Value))
            {
                c.InLink = true;
            }
            else if (c.InLink && Regexes.Lexers.EndHtmlLink.IsMatch(match.Value))
            {
                c.InLink = false;
            }
            return new MarkdownTagInlineToken(this, match.Value);
        }
    }
}
