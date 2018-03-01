// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownHtmlBlockRule : IMarkdownRule
    {
        private static readonly Matcher InlineElementNames =
            (Matcher.CaseInsensitiveString("a") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("em") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("strong") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("small") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("s") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("cite") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("q") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("dfn") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("abbr") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("data") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("time") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("code") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("var") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("samp") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("kbd") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("sub") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("sup") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("i") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("b") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("u") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("mark") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("ruby") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("rt") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("rp") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("bdi") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("bdo") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("span") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("br") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("wbr") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("ins") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("del") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("img") + Matcher.WordBoundary) |
            (Matcher.CaseInsensitiveString("tu") + Matcher.WordBoundary);
        private static readonly Matcher _ElementName =
            InlineElementNames.ToNegativeTest() +
            // \w+
            Matcher.AnyWordCharacter.RepeatAtLeast(1) +
            Matcher.Char(':').ToNegativeTest() +
            // (?!:\/|[^\w\s@]*@)
            (
                Matcher.String(":/") |
                ((Matcher.AnyWordCharacter | Matcher.AnyCharIn(' ', '\n', '@')).ToNegativeTest() + Matcher.AnyChar).RepeatAtLeast(0) + '@'
            ).ToNegativeTest() +
            // \b
            (Matcher.AnyWordCharacter.ToNegativeTest() | Matcher.EndOfString);
        //  *(?:\n{2,}|$)
        private static readonly Matcher _EndSymbol =
            Matcher.WhiteSpacesOrEmpty +
            (Matcher.NewLine.RepeatAtLeast(2) | (Matcher.NewLine.Maybe() + Matcher.EndOfString));
        private static readonly Matcher _HtmlMatcher =
            Matcher.WhiteSpacesOrEmpty +
            (
                // html comment
                (
                    Matcher.String("<!--") +
                    (
                        Matcher.AnyCharNot('-').RepeatAtLeast(1) |
                        (Matcher.Char('-') + Matcher.String("->").ToNegativeTest())
                    ) + "-->"
                ) |
                // none-empty element
                (
                    Matcher.Char('<') +
                    _ElementName.ToGroup("element") +
                    // [\s\S]+?<\/\1>
                    (
                        Matcher.AnyCharNot('<').RepeatAtLeast(1) |
                        (Matcher.Char('<') + (Matcher.Char('/') + Matcher.BackReference("element") + '>' + _EndSymbol).ToNegativeTest())
                    ).RepeatAtLeast(1) +
                    Matcher.String("</") + Matcher.BackReference("element") + '>'
                ) |
                // empty element
                (
                    Matcher.Char('<') +
                    _ElementName +
                    // (?:"[^"]*"|'[^']*'|[^'"">])*
                    (
                        Matcher.AnyCharNotIn('"', '\'', '>').RepeatAtLeast(1) |
                        (Matcher.Char('"') + Matcher.AnyCharNot('"').RepeatAtLeast(0) + '"') |
                        (Matcher.Char('\'') + Matcher.AnyCharNot('\'').RepeatAtLeast(0) + '\'')
                    ).RepeatAtLeast(0) +
                    '>'
                )
            ) +
            _EndSymbol;

        public virtual string Name => "Html";

        [Obsolete("Please use HtmlMatcher.")]
        public virtual Regex Html => Regexes.Block.Html;

        public virtual Matcher HtmlMatcher => _HtmlMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (context.IsInParagraph)
            {
                return null;
            }
            if (Html != Regexes.Block.Html || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(HtmlMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);

                var elementName = match.GetGroup("element")?.GetValue();
                bool isPre = parser.Options.Sanitizer == null &&
                    ("pre".Equals(elementName, StringComparison.OrdinalIgnoreCase) || "script".Equals(elementName, StringComparison.OrdinalIgnoreCase) || "style".Equals(elementName, StringComparison.OrdinalIgnoreCase));
                if (parser.Options.Sanitize)
                {
                    return new TwoPhaseBlockToken(
                        this,
                        parser.Context,
                        sourceInfo,
                        (p, t) => new MarkdownParagraphBlockToken(
                            t.Rule,
                            t.Context,
                            p.TokenizeInline(t.SourceInfo),
                            t.SourceInfo));
                }
                else
                {
                    return new TwoPhaseBlockToken(
                        this,
                        parser.Context,
                        sourceInfo,
                        (p, t) =>
                        {
                            InlineContent ic;
                            if (isPre)
                            {
                                ic = new InlineContent(
                                    ImmutableArray.Create<IMarkdownToken>(
                                        new MarkdownRawToken(
                                            this,
                                            parser.Context,
                                            t.SourceInfo)));
                            }
                            else
                            {
                                var c = new MarkdownInlineContext(
                                    ImmutableList.Create<IMarkdownRule>(
                                        new MarkdownPreElementInlineRule(),
                                        new MarkdownTagInlineRule(),
                                        new MarkdownTextInlineRule()));
                                p.SwitchContext(c);
                                ic = new InlineContent(p.Tokenize(t.SourceInfo));
                                p.SwitchContext(t.Context);
                            }
                            return new MarkdownHtmlBlockToken(
                                t.Rule,
                                t.Context,
                                ic,
                                t.SourceInfo);
                        });
                }
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
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
