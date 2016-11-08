// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public static class TokenHelper
    {
        public static ImmutableArray<IMarkdownToken> CreateParagraghs(
            IMarkdownParser parser,
            IMarkdownRule rule,
            ImmutableArray<IMarkdownToken> blockTokens,
            bool wrapParagraph,
            SourceInfo sourceInfo)
        {
            var result = new List<IMarkdownToken>(blockTokens.Length);
            var textContent = StringBuffer.Empty;
            var si = sourceInfo;
            foreach (var token in blockTokens)
            {
                var text = token as MarkdownTextToken;
                if (text != null)
                {
                    if (textContent != StringBuffer.Empty)
                    {
                        textContent += "\n";
                    }
                    else
                    {
                        si = text.SourceInfo;
                    }
                    textContent += text.Content;
                    continue;
                }
                var newLine = token as MarkdownNewLineBlockToken;
                if (newLine?.SourceInfo.Markdown.Length == 1)
                {
                    continue;
                }
                if (textContent != StringBuffer.Empty)
                {
                    var rawMarkdown = textContent.ToString();
                    result.Add(CreateTwoPhaseToken(parser, rule, wrapParagraph, si.Copy(rawMarkdown)));
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
                result.Add(CreateTwoPhaseToken(parser, rule, wrapParagraph, si.Copy(rawMarkdown)));
            }
            return result.ToImmutableArray();
        }

        private static TwoPhaseBlockToken CreateTwoPhaseToken(IMarkdownParser parser, IMarkdownRule rule, bool wrapParagraph, SourceInfo sourceInfo)
        {
            var inlineContent = parser.TokenizeInline(sourceInfo);
            if (wrapParagraph)
            {
                return new TwoPhaseBlockToken(
                    rule,
                    parser.Context,
                    sourceInfo,
                    (p, t) => new MarkdownParagraphBlockToken(t.Rule, p.Context, p.TokenizeInline(t.SourceInfo), t.SourceInfo));
            }
            else
            {
                return new TwoPhaseBlockToken(
                    rule,
                    parser.Context,
                    sourceInfo,
                    (p, t) => new MarkdownNonParagraphBlockToken(t.Rule, p.Context, p.TokenizeInline(t.SourceInfo), t.SourceInfo));
            }
        }
    }
}
