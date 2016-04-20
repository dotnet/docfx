// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.using System;

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownCommentInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Comment";

        public virtual Regex Comment => Regexes.Inline.Comment;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, ref string source)
        {
            var match = Comment.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownRawToken(this, parser.Context, match.Value);
        }
    }
}
