// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownCodeElementInlineRule : IMarkdownRule
    {
        public string Name => "Inline.CodeElement";

        public virtual Regex CodeElement => Regexes.Inline.CodeElement;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = CodeElement.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownRawToken(this, engine.Context, match.Value);
        }
    }
}
