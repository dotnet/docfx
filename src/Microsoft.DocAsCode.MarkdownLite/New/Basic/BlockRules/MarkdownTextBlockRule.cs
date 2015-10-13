namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownTextBlockRule : IMarkdownRule
    {
        public string Name => "Text";

        public virtual Regex Text => Regexes.Block.Text;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Text.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new MarkdownTextToken(this, match.Value);
        }
    }
}
