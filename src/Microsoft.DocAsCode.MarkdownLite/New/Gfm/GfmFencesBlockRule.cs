namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class GfmFencesBlockRule : IMarkdownRule
    {
        public string Name => "Fences";

        public virtual Regex Fences => Regexes.Block.Gfm.Fences;

        public IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Fences.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownCodeBlockToken(this, match.Groups[3].Value, match.Groups[2].Value);
        }
    }
}
