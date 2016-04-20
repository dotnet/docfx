// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownEscapedTextInlineRule : IMarkdownRule
    {
        public string Name => "Inline.EscapedText";

        public virtual Regex EscapedText => Regexes.Inline.EscapedText;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, ref string source)
        {
            var match = EscapedText.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return new MarkdownTextToken(this, parser.Context, StringHelper.Escape(match.Groups[1].Value), match.Value);
        }
    }
}
