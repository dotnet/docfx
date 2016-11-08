// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownParagraphBlockRule : IMarkdownRule
    {
        public static readonly MarkdownParagraphBlockRule Instance = new MarkdownParagraphBlockRule();

        private MarkdownParagraphBlockRule() { }

        public string Name => "Paragraph";

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            return null;
        }
    }
}
