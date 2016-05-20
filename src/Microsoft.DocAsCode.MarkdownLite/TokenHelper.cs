// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public static class TokenHelper
    {
        public static ImmutableArray<IMarkdownToken> ParseInlineToken(
            IMarkdownParser parser,
            IMarkdownRule rule,
            ImmutableArray<IMarkdownToken> blockTokens,
            bool wrapParagraph,
            LineInfo lineInfo)
        {
            var result = new List<IMarkdownToken>(blockTokens.Length);
            var textContent = StringBuffer.Empty;
            foreach (var token in blockTokens)
            {
                var text = token as MarkdownTextToken;
                if (text != null)
                {
                    if (textContent != StringBuffer.Empty)
                    {
                        textContent += "\n";
                    }
                    textContent += text.Content;
                    continue;
                }
                var newLine = token as MarkdownNewLineBlockToken;
                if (newLine?.RawMarkdown.Length == 1)
                {
                    continue;
                }
                if (textContent != StringBuffer.Empty)
                {
                    var rawMarkdown = textContent.ToString();
                    result.Add(CreateToken(parser, rule, wrapParagraph, rawMarkdown, lineInfo));
                    textContent = StringBuffer.Empty;
                }
                if (newLine != null)
                {
                    continue;
                }
                result.Add(token);
            }
            if (textContent != StringBuffer.Empty)
            {
                var rawMarkdown = textContent.ToString();
                result.Add(CreateToken(parser, rule, wrapParagraph, rawMarkdown, lineInfo));
            }
            return result.ToImmutableArray();
        }

        private static IMarkdownToken CreateToken(IMarkdownParser parser, IMarkdownRule rule, bool wrapParagraph, string rawMarkdown, LineInfo lineInfo)
        {
            var inlineContent = parser.TokenizeInline(rawMarkdown, lineInfo);
            if (wrapParagraph)
            {
                return new MarkdownParagraphBlockToken(rule, parser.Context, inlineContent, rawMarkdown, lineInfo);
            }
            else
            {
                return new MarkdownNonParagraphBlockToken(rule, parser.Context, inlineContent, rawMarkdown, lineInfo);
            }
        }
    }
}
