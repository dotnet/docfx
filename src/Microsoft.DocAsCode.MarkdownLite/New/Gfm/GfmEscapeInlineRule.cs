namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class GfmEscapeInlineRule : MarkdownEscapeInlineRule
    {
        public override Regex Escape => Regexes.Inline.Gfm.Escape;
    }
}
