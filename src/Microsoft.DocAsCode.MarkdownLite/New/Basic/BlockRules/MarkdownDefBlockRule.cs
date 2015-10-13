namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownDefBlockRule : IMarkdownRule
    {
        public string Name => "Def";

        public virtual Regex Def => Regexes.Block.Def;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Def.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            engine.Links[match.Groups[1].Value.ToLower()] = new LinkObj
            {
                Href = match.Groups[2].Value,
                Title = match.Groups[3].Value
            };
            return new MarkdownIgnoreToken(this);
        }
    }
}
