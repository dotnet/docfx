namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    public class MarkdownAutoLinkInlineRule : IMarkdownRule
    {
        public string Name => "Inline.AutoLink";
        private int _mangleCounter;

        public virtual Regex AutoLink => Regexes.Inline.AutoLink;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = AutoLink.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            StringBuffer text;
            StringBuffer href;
            if (match.Groups[2].Value == "@")
            {
                text = match.Groups[1].Value[6] == ':'
                  ? Mangle(engine.Options.Mangle, match.Groups[1].Value.Substring(7))
                  : Mangle(engine.Options.Mangle, match.Groups[1].Value);
                href = Mangle(engine.Options.Mangle, "mailto:") + text;
            }
            else
            {
                text = StringHelper.Escape(match.Groups[1].Value);
                href = text;
            }

            return new MarkdownLinkInlineToken(this, href, null, text);
        }

        private StringBuffer Mangle(bool enableMangle, string text)
        {
            if (enableMangle)
            {
                return Mangle(text);
            }
            else
            {
                return text;
            }
        }

        protected virtual StringBuffer Mangle(string text)
        {
            var result = StringBuffer.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i].ToString();
                if ((_mangleCounter++ & 1) == 0)
                {
                    result = result + "&#x" + Convert.ToString(ch[0], 16) + ";";
                }
                else
                {
                    result = result + "&#" + ch + ";";
                }
            }
            return result;
        }

    }
}
