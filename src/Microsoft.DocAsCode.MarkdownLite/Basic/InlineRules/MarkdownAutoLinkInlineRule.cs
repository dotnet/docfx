// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    public class MarkdownAutoLinkInlineRule : IMarkdownRule
    {
        private int _mangleCounter;

        public virtual string Name => "Inline.AutoLink";

        public virtual Regex AutoLink => Regexes.Inline.AutoLink;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (MarkdownInlineContext.GetIsInLink(parser.Context))
            {
                return null;
            }
            var match = AutoLink.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);

            StringBuffer text;
            StringBuffer href;
            if (match.Groups[2].Value == "@")
            {
                text = match.Groups[1].Value[6] == ':'
                  ? Mangle(parser.Options.Mangle, match.Groups[1].Value.Substring(7))
                  : Mangle(parser.Options.Mangle, match.Groups[1].Value);
                href = Mangle(parser.Options.Mangle, "mailto:") + text;
            }
            else
            {
                text = StringHelper.Escape(match.Groups[1].Value);
                href = match.Groups[1].Value;
            }

            return new MarkdownLinkInlineToken(
                this, 
                parser.Context, 
                href, 
                null, 
                ImmutableArray.Create<IMarkdownToken>(
                    new MarkdownRawToken(this, parser.Context, sourceInfo.Copy(text))),
                sourceInfo,
                MarkdownLinkType.AutoLink,
                null);
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
