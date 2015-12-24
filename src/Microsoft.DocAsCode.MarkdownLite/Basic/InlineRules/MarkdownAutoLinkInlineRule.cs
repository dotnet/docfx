// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    public class MarkdownAutoLinkInlineRule : IMarkdownRule
    {
        public string Name => "Inline.AutoLink";
        private int _mangleCounter;

        public virtual Regex AutoLink => Regexes.Inline.AutoLink;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
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

            return new MarkdownLinkInlineToken(
                this, 
                engine.Context, 
                href, 
                null, 
                ImmutableArray<IMarkdownToken>.Empty.Add(
                    new MarkdownRawToken(this, engine.Context, text)),
                match.Value);
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
                var ch = text[i];
                if ((_mangleCounter++ & 1) == 0)
                {
                    result = result + "&#x" + Convert.ToString(ch, 16) + ";";
                }
                else
                {
                    result = result + "&#" + Convert.ToString(ch, 10) + ";";
                }
            }
            return result;
        }

    }
}
