namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class GfmParagraphBlockRule : MarkdownParagraphBlockRule
    {
        public override Regex Paragraph => Regexes.Block.Gfm.Paragraph;
    }
}
