// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    public class MarkdownHtmlBlockRule : IMarkdownRule
    {
        public virtual string Name => "Html";

        public virtual Regex Html => Regexes.Block.Html;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParserContext context)
        {
            var match = Html.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo = context.LineInfo;
            context.Consume(match.Length);

            bool isPre = parser.Options.Sanitizer == null &&
                (match.Groups[1].Value == "pre" || match.Groups[1].Value == "script" || match.Groups[1].Value == "style");
            if (parser.Options.Sanitize)
            {
                return new TwoPhaseBlockToken(
                    this,
                    parser.Context,
                    match.Value,
                    lineInfo,
                    (p, t) => new MarkdownParagraphBlockToken(
                        t.Rule,
                        t.Context,
                        p.TokenizeInline(match.Value, lineInfo),
                        t.RawMarkdown,
                        t.LineInfo));
            }
            else
            {
                return new TwoPhaseBlockToken(
                    this,
                    parser.Context,
                    match.Value,
                    lineInfo,
                    (p, t) => new MarkdownHtmlBlockToken(
                        t.Rule,
                        t.Context,
                        isPre ?
                            new InlineContent(
                                ImmutableArray.Create<IMarkdownToken>(
                                    new MarkdownRawToken(
                                        this,
                                        parser.Context,
                                        t.RawMarkdown,
                                        t.LineInfo)))
                        :
                            p.TokenizeInline(t.RawMarkdown, t.LineInfo),
                        t.RawMarkdown,
                        t.LineInfo));
            }
        }
    }
}
