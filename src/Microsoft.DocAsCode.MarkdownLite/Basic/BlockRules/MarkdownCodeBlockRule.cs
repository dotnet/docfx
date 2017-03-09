// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using Matchers;

    public class MarkdownCodeBlockRule : IMarkdownRule
    {
        private static readonly Matcher CodeMatcher =
            Matcher.Repeat(
                Matcher.Sequence(
                    Matcher.Repeat(
                        Matcher.Char(' '),
                        4
                    ),
                    Matcher.Repeat(
                        Matcher.AnyCharNotIn('\n'),
                        1
                    ),
                    Matcher.Repeat(
                        Matcher.Char('\n'),
                        0
                    )
                ),
                1
            );

        public virtual string Name => "Code";

        public virtual Matcher Code => CodeMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (context.IsInParagraph)
            {
                return null;
            }
            var match = context.Match(Code);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                var capStr = Regexes.Lexers.LeadingWhiteSpaces.Replace(sourceInfo.Markdown, string.Empty);
                if (parser.Options.Pedantic)
                {
                    return new MarkdownCodeBlockToken(this, parser.Context, capStr, null, sourceInfo);
                }
                else
                {
                    return new MarkdownCodeBlockToken(this, parser.Context, Regexes.Lexers.TailingEmptyLines.Replace(capStr, string.Empty), null, sourceInfo);
                }
            }
            return null;
        }
    }
}
