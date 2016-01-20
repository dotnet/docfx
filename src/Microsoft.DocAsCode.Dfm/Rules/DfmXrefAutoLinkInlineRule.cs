// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    /// <summary>
    /// Xref auto link syntax: 
    /// 1. `<xref:uid>`
    /// 2. `<xref:"uid with space">`
    /// </summary>
    public class DfmXrefAutoLinkInlineRule : IMarkdownRule
    {
        private static readonly Regex XrefAutoLinkRegex = new Regex(@"^(<xref:([^ >]+)>)", RegexOptions.Compiled);
        private static readonly Regex XrefAutoLinkRegexWithQuote = new Regex(@"^<xref:(['""])(\s*?\S+?[\s\S]*?)\1>", RegexOptions.Compiled);

        public string Name => "XrefAutoLink";

        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
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
            return new DfmXrefInlineToken(this, engine.Context, content, null, null, true, match.Value);
        }
    }
}
