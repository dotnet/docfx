namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownTextInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Text";

        public virtual Regex Text => Regexes.Inline.Text;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Text.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownTextToken(this, StringHelper.Escape(Smartypants(engine.Options, match.Groups[0].Value)));
        }

        /// <summary>
        /// Smartypants Transformations
        /// </summary>
        protected virtual string Smartypants(Options options, string text)
        {
            if (!options.Smartypants)
            {
                return text;
            }

            return text
                // em-dashes
                .Replace("---", "\u2014")
                // en-dashes
                .Replace("--", "\u2013")
                // opening singles
                .ReplaceRegex(Regexes.Inline.Smartypants.OpeningSingles, "$1\u2018")
                // closing singles & apostrophes
                .Replace("'", "\u2019")
                // opening doubles
                .ReplaceRegex(Regexes.Inline.Smartypants.OpeningDoubles, "$1\u201c")
                // closing doubles
                .Replace("\"", "\u201d")
                // ellipses
                .Replace("...", "\u2026");
        }

    }
}
