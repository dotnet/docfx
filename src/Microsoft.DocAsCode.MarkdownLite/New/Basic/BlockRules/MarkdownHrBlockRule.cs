namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownHrBlockRule : IMarkdownRule
    {
        public string Name => "Hr";

        public virtual Regex Hr => Regexes.Block.Hr;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Hr.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new MarkdownHrBlockToken(this);
        }
    }
}
