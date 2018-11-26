// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownCodeBlockRule : IMarkdownRule
    {
        private static readonly Matcher _CodeMatcher =
            Matcher.WhiteSpace.RepeatAtLeast(4) +
            Matcher.AnyStringInSingleLine +
            (
                Matcher.NewLine.RepeatAtLeast(1) +
                Matcher.WhiteSpace.RepeatAtLeast(4) +
                Matcher.AnyStringInSingleLine
            ).RepeatAtLeast(0) +
            Matcher.NewLine.Maybe();

        public virtual string Name => "Code";

        [Obsolete("Please use CodeMatcher.")]
        public virtual Regex Code => Regexes.Block.Code;

        public virtual Matcher CodeMatcher => _CodeMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (context.IsInParagraph)
            {
                return null;
            }
            if (Code != Regexes.Block.Code)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(CodeMatcher);
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
                    return new MarkdownCodeBlockToken(this, parser.Context, Regexes.Lexers.TailingEmptyLine.Replace(capStr, string.Empty), null, sourceInfo);
                }
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Code.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            var capStr = Regexes.Lexers.LeadingWhiteSpaces.Replace(match.Value, string.Empty);
            if (parser.Options.Pedantic)
            {
                return new MarkdownCodeBlockToken(this, parser.Context, capStr, null, sourceInfo);
            }
            else
            {
                return new MarkdownCodeBlockToken(this, parser.Context, Regexes.Lexers.TailingEmptyLine.Replace(capStr, string.Empty), null, sourceInfo);
            }
        }
    }
}
