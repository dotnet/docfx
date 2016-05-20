// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.using System;

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownCommentInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Comment";

        public virtual Regex Comment => Regexes.Inline.Comment;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Comment.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo = context.LineInfo;
            context.Consume(match.Length);

            return new MarkdownRawToken(this, parser.Context, match.Value, lineInfo);
        }
    }
}
