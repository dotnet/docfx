// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    /// <summary>
    /// Xref auto link syntax: 
    /// 1. `&lt;xref:uid>`
    /// 2. `&lt;xref:"uid with space">`
    /// </summary>
    public class DfmXrefAutoLinkInlineRule : IMarkdownRule
    {
        public static readonly string XrefAutoLinkRegexString = @"(<xref:([^ >]+)>)";
        public static readonly string XrefAutoLinkRegexWithQuoteString = @"<xref:(['""])(\s*?\S+?[\s\S]*?)\1>";

        private static readonly Regex XrefAutoLinkRegex = new Regex("^" + XrefAutoLinkRegexString, RegexOptions.Compiled, TimeSpan.FromSeconds(10));
        private static readonly Regex XrefAutoLinkRegexWithQuote = new Regex("^" + XrefAutoLinkRegexWithQuoteString, RegexOptions.Compiled, TimeSpan.FromSeconds(10));

        public string Name => "DfmXrefAutoLink";

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (MarkdownInlineContext.GetIsInLink(parser.Context))
            {
                return null;
            }
            var match = XrefAutoLinkRegexWithQuote.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                match = XrefAutoLinkRegex.Match(context.CurrentMarkdown);
                if (match.Length == 0)
                {
                    return null;
                }
            }
            var sourceInfo = context.Consume(match.Length);
            var content = match.Groups[2].Value;
            return new DfmXrefInlineToken(this, parser.Context, content, ImmutableArray<IMarkdownToken>.Empty, null, true, sourceInfo);
        }
    }
}
