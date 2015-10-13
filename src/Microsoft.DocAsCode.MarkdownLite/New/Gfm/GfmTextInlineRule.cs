using System.Text.RegularExpressions;

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class GfmTextInlineRule : MarkdownTextInlineRule
    {
        public override Regex Text => Regexes.Inline.Gfm.Text;
    }
}
