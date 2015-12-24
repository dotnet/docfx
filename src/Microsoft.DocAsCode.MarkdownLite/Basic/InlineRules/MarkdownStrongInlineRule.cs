// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownStrongInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Strong";

        public virtual Regex Strong => Regexes.Inline.Strong;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Strong.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownStrongInlineToken(this, engine.Context, engine.Tokenize(match.NotEmpty(2, 1)), match.Value);
        }
    }
}
