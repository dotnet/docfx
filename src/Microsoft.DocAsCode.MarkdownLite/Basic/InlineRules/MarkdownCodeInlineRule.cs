// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownCodeInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Code";

        public virtual Regex Code => Regexes.Inline.Code;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Code.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownCodeInlineToken(this, engine.Context, match.Groups[2].Value);
        }
    }
}
