// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmIncludeBlockRule : IMarkdownRule
    {
        private static readonly Regex _incRegex = new Regex($"{DocfxFlavoredIncHelper.InlineIncRegexString}\\s*(\\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public virtual string Name => "INCLUDE";
        public virtual Regex Include => _incRegex;

        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Include.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // [!include[title](path "optionalTitle")]
            // 1. Get include file path 
            var path = match.Groups[2].Value;

            // 2. Get title
            var value = match.Groups[1].Value;
            var title = match.Groups[4].Value;

            return new DfmIncludeBlockToken(this, engine.Context, path, value, title, match.Groups[0].Value, match.Value);
        }
    }
}
