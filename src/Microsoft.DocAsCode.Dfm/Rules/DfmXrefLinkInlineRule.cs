// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    /// <summary>
    /// Xref Link syntax:
    /// 1. `[name](xref:uid "title")`
    /// 2. `[name](@uid "title")`
    /// title can be omitted
    /// </summary>
    public class DfmXrefLinkInlineRule : IMarkdownRule
    {
        private static readonly Regex XrefLinkRegex = new Regex(@"^\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\] *\(\s*<?(?:xref:|@)(\s*?\S+?[\s\S]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)", RegexOptions.Compiled);
        public string Name => "XrefLink";
        
        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = XrefLinkRegex.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            var name = match.Groups[1].Value;
            var xref = match.Groups[2].Value;
            var title = match.Groups[4].Value;

            return new DfmXrefInlineToken(this, engine.Context, xref, name, title, true, match.Value);
        }
    }
}
