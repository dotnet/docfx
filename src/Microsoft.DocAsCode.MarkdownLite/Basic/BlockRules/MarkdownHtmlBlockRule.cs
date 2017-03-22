// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownHtmlBlockRule : IMarkdownRule
    {
        private static readonly Matcher InlineElementNames =
            Matcher.CaseInsensitiveString("a") |
            Matcher.CaseInsensitiveString("em") |
            Matcher.CaseInsensitiveString("strong") |
            Matcher.CaseInsensitiveString("small") |
            Matcher.CaseInsensitiveString("s") |
            Matcher.CaseInsensitiveString("cite") |
            Matcher.CaseInsensitiveString("q") |
            Matcher.CaseInsensitiveString("dfn") |
            Matcher.CaseInsensitiveString("abbr") |
            Matcher.CaseInsensitiveString("data") |
            Matcher.CaseInsensitiveString("time") |
            Matcher.CaseInsensitiveString("code") |
            Matcher.CaseInsensitiveString("var") |
            Matcher.CaseInsensitiveString("samp") |
            Matcher.CaseInsensitiveString("kbd") |
            Matcher.CaseInsensitiveString("sub") |
            Matcher.CaseInsensitiveString("sup") |
            Matcher.CaseInsensitiveString("i") |
            Matcher.CaseInsensitiveString("b") |
            Matcher.CaseInsensitiveString("u") |
            Matcher.CaseInsensitiveString("mark") |
            Matcher.CaseInsensitiveString("ruby") |
            Matcher.CaseInsensitiveString("rt") |
            Matcher.CaseInsensitiveString("rp") |
            Matcher.CaseInsensitiveString("bdi") |
            Matcher.CaseInsensitiveString("bdo") |
            Matcher.CaseInsensitiveString("span") |
            Matcher.CaseInsensitiveString("br") |
            Matcher.CaseInsensitiveString("wbr") |
            Matcher.CaseInsensitiveString("ins") |
            Matcher.CaseInsensitiveString("del") |
            Matcher.CaseInsensitiveString("img");
        private static readonly Matcher _ElementName =
            (InlineElementNames + Matcher.AnyWordCharacter.ToNegativeTest()).ToNegativeTest() +
            // \w+
            Matcher.AnyWordCharacter.RepeatAtLeast(1) +
            // (?!:\/|[^\w\s@]*@)
            (
                Matcher.String(":/") |
                ((Matcher.AnyWordCharacter | Matcher.AnyCharIn(' ', '\n', '@')).ToNegativeTest() + Matcher.AnyChar).RepeatAtLeast(0) + '@'
            ).ToNegativeTest() +
            // \b
            (Matcher.AnyWordCharacter.ToNegativeTest() | Matcher.EndOfString);
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
                        (Matcher.Char('<') + ((Matcher.Char('/') + Matcher.BackReference("element") + '>' + Matcher.WhiteSpacesOrEmpty + (Matcher.NewLine | Matcher.EndOfString)).ToNegativeTest()))
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
            //  *(?:\n{2,}|$)
            Matcher.WhiteSpacesOrEmpty + (Matcher.NewLine.RepeatAtLeast(2) | (Matcher.NewLine.RepeatAtLeast(0) + Matcher.EndOfString));

        public virtual string Name => "Html";

        [Obsolete("Please use HtmlMatcher.")]
        public virtual Regex Html => Regexes.Block.Html;

        public virtual Matcher HtmlMatcher => _HtmlMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (Html != Regexes.Block.Html)
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
