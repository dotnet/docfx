// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownTextBlockRule : IMarkdownRule
    {
        public string Name => "Text";

        public virtual Regex Text => Regexes.Block.Text;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, ref string source)
        {
            if ((bool)parser.Context.Variables[MarkdownBlockContext.IsTop])
            {
                return null;
            }
            var match = Text.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new MarkdownTextToken(this, parser.Context, match.Value, match.Value);
        }
    }
}
