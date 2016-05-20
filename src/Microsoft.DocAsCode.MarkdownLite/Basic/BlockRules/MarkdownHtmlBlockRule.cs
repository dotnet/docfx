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

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Html.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);

            bool isPre = parser.Options.Sanitizer == null &&
                (match.Groups[1].Value == "pre" || match.Groups[1].Value == "script" || match.Groups[1].Value == "style");
            if (parser.Options.Sanitize)
            {
                return new TwoPhaseBlockToken(
                    this,
                    parser.Context,
                    sourceInfo,
                    (p, t) => new MarkdownParagraphBlockToken(
                        t.Rule,
                        t.Context,
                        p.TokenizeInline(t.SourceInfo.Copy(match.Value)),
                        t.SourceInfo));
            }
            else
            {
                return new TwoPhaseBlockToken(
                    this,
                    parser.Context,
                    sourceInfo,
                    (p, t) => new MarkdownHtmlBlockToken(
                        t.Rule,
                        t.Context,
                        isPre ?
                            new InlineContent(
                                ImmutableArray.Create<IMarkdownToken>(
                                    new MarkdownRawToken(
                                        this,
                                        parser.Context,
                                        t.SourceInfo)))
                        :
                            p.TokenizeInline(t.SourceInfo),
                        t.SourceInfo));
            }
        }
    }
}
