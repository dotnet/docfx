﻿// Copyright (c) Microsoft. All rights reserved.
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
                    if (textContent == StringBuffer.Empty)
                    {
                        si = text.SourceInfo;
                    }
                    textContent += text.Content;
                    continue;
                }
                if (textContent != StringBuffer.Empty)
                {
                    result.Add(GroupTextTokens(parser, rule, wrapParagraph, textContent, si));
                    textContent = StringBuffer.Empty;
                }
                if (token is MarkdownNewLineBlockToken)
                {
                    continue;
                }
                result.Add(token);
            }
            if (textContent != StringBuffer.Empty)
            {
                result.Add(GroupTextTokens(parser, rule, wrapParagraph, textContent, si));
            }
            return result.ToImmutableArray();
        }

        private static IMarkdownToken GroupTextTokens(IMarkdownParser parser, IMarkdownRule rule, bool wrapParagraph, StringBuffer textContent, SourceInfo si)
        {
            if (textContent.EndsWith('\n'))
            {
                textContent = textContent.Substring(0, textContent.GetLength() - 1);
            }
            var rawMarkdown = textContent.ToString();
            return CreateTwoPhaseToken(parser, rule, wrapParagraph, si.Copy(rawMarkdown));
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
