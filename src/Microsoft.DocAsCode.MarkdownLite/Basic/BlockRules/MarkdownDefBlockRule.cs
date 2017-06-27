// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownDefBlockRule : IMarkdownRule
    {
        private static readonly Matcher _DefMatcher =
            Matcher.WhiteSpacesOrEmpty +
            '[' + Matcher.AnyCharNot(']').RepeatAtLeast(1).ToGroup("key") + "]:" +
            Matcher.WhiteSpacesOrEmpty +
            (
                (Matcher.Char('<') + Matcher.AnyCharNotIn('>', ' ', '\n').RepeatAtLeast(1).ToGroup("href") + '>') |
                Matcher.AnyCharNotIn(' ', '\n').RepeatAtLeast(1).ToGroup("href")
            ) +
            (
                Matcher.WhiteSpaces +
                (
                    (Matcher.Char('"') + Matcher.AnyCharNot('"').RepeatAtLeast(0).ToGroup("title") + '"') |
                    (Matcher.Char('(') + Matcher.AnyCharNot(')').RepeatAtLeast(0).ToGroup("title") + ')')
                )
            ).Maybe() +
            Matcher.WhiteSpacesOrEmpty + (Matcher.NewLine | Matcher.EndOfString);

        public virtual string Name => "Def";

        [Obsolete("Please use DefMatcher.")]
        public virtual Regex Def => Regexes.Block.Def;

        public virtual Matcher DefMatcher => _DefMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (Def != Regexes.Block.Def || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(DefMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                parser.Links[match["key"].GetValue().ToLower()] = new LinkObj
                {
                    Href = StringHelper.UnescapeMarkdown(match["href"].GetValue()),
                    Title = StringHelper.UnescapeMarkdown(match.GetGroup("title")?.GetValue() ?? string.Empty),
                };
                return new MarkdownIgnoreToken(this, parser.Context, sourceInfo);
            }
            return null;
        }

        [Obsolete]
        public virtual IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (!parser.Context.Variables.ContainsKey(MarkdownBlockContext.IsTop) ||
                false.Equals(parser.Context.Variables[MarkdownBlockContext.IsTop]))
            {
                return null;
            }
            var match = Def.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            parser.Links[match.Groups[1].Value.ToLower()] = new LinkObj
            {
                Href = StringHelper.LegacyUnescapeMarkdown(match.Groups[2].Value),
                Title = StringHelper.LegacyUnescapeMarkdown(match.Groups[3].Value),
            };
            return new MarkdownIgnoreToken(this, parser.Context, sourceInfo);
        }
    }
}
