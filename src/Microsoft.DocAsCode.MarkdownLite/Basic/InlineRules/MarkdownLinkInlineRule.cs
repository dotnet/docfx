// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownLinkInlineRule : MarkdownLinkBaseInlineRule
    {
        public override string Name => "Inline.Link";

        public virtual Regex Link => Regexes.Inline.Link;

        public override IMarkdownToken TryMatch(IMarkdownParser parser, ref string source)
        {
            var match = Link.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            return GenerateToken(parser, match.Groups[2].Value, match.Groups[4].Value, match.Groups[1].Value, match.Value[0] == '!', match.Value);
        }
    }
}
