// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownEscapeInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Escape";

        public virtual Regex Escape => Regexes.Inline.Escape;

        public virtual IMarkdownToken TryMatch(MarkdownParser engine, ref string source)
        {
            var match = Escape.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new MarkdownEscapeInlineToken(this, engine.Context, match.Groups[1].Value);
        }
    }
}
