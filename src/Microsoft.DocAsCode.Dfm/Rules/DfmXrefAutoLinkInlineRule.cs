// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
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

        private static readonly Regex XrefAutoLinkRegex = new Regex("^" + XrefAutoLinkRegexString, RegexOptions.Compiled);
        private static readonly Regex XrefAutoLinkRegexWithQuote = new Regex("^" + XrefAutoLinkRegexWithQuoteString, RegexOptions.Compiled);

        public string Name => "XrefAutoLink";

        public IMarkdownToken TryMatch(IMarkdownParser parser, ref string source)
        {
            var match = XrefAutoLinkRegexWithQuote.Match(source);
            if (match.Length == 0)
            {
                match = XrefAutoLinkRegex.Match(source);
                if (match.Length == 0)
                {
                    return null;
                }
            }

            source = source.Substring(match.Length);

            var content = match.Groups[2].Value;
            return new DfmXrefInlineToken(this, parser.Context, content, ImmutableArray<IMarkdownToken>.Empty, null, true, match.Value);
        }
    }
}
