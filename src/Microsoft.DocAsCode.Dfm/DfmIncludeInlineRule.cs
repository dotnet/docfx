// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmIncludeInlineRule : IMarkdownRule
    {
        public virtual string Name => "INCLUDE";
        private static readonly Regex _inlineIncludeRegex = new Regex(DocfxFlavoredIncHelper.InlineIncRegexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public virtual Regex Include => _inlineIncludeRegex;

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

            // 3. Apply inline rules to the included content
            return new DfmIncludeInlineToken(this, engine.Context, path, value, title, match.Groups[0].Value, match.Value);
        }
    }
}
