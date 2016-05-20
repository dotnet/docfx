// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class GfmFencesBlockRule : IMarkdownRule
    {
        public string Name => "Fences";

        public virtual Regex Fences => Regexes.Block.Gfm.Fences;

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Fences.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo= context.LineInfo;
            context.Consume(match.Length);

            return new MarkdownCodeBlockToken(this, parser.Context, match.Groups[3].Value, match.Value, match.Groups[2].Value, lineInfo);
        }
    }
}
