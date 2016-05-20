// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownParagraphBlockRule : IMarkdownRule
    {
        public virtual string Name => "Paragraph";

        public virtual Regex Paragraph => Regexes.Block.Paragraph;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (!(bool)parser.Context.Variables[MarkdownBlockContext.IsTop])
            {
                return null;
            }
            var match = Paragraph.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo = context.LineInfo;
            context.Consume(match.Length);
            var content = match.Groups[1].Value[match.Groups[1].Value.Length - 1] == '\n'
                ? match.Groups[1].Value.Substring(0, match.Groups[1].Value.Length - 1)
                : match.Groups[1].Value;
            return new TwoPhaseBlockToken(
                this,
                parser.Context,
                match.Value,
                lineInfo,
                (p, t) => new MarkdownParagraphBlockToken(
                    t.Rule,
                    t.Context,
                    p.TokenizeInline(content, t.LineInfo),
                    t.RawMarkdown,
                    t.LineInfo));
        }
    }
}
