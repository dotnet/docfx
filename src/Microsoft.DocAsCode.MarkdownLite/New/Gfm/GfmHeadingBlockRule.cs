namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class GfmHeadingBlockRule : MarkdownHeadingBlockRule
    {
        public override Regex Heading => Regexes.Block.Gfm.Heading;
    }
}
