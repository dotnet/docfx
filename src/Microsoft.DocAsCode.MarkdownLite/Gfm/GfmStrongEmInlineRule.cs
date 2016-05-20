// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    public class GfmStrongEmInlineRule : IMarkdownRule
    {
        public virtual string Name => "Inline.Gfm.StringEm";

        public virtual Regex StrongEm => Regexes.Inline.Gfm.StrongEm;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = StrongEm.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            if (match.Groups[1].Length > 0)
            {
                var sourceInfo = context.Consume(match.Groups[1].Length);
                return new MarkdownTextToken(this, parser.Context, match.Groups[1].Value, sourceInfo);
            }
            else
            {
                var sourceInfo = context.Consume(match.Length);
                return new MarkdownStrongInlineToken(
                    this,
                    parser.Context,
                    GetContent(parser, match, sourceInfo),
                    sourceInfo);
            }
        }

        private ImmutableArray<IMarkdownToken> GetContent(IMarkdownParser parser, Match match, SourceInfo sourceInfo)
        {
            var emContent = new MarkdownEmInlineToken(
                this,
                parser.Context,
                parser.Tokenize(sourceInfo.Copy(match.Groups[2].Value)),
                sourceInfo.Copy("*" + match.Groups[1].Value + "*"));

            if (match.Groups[2].Length > 0)
            {
                return parser.Tokenize(sourceInfo.Copy(match.Groups[3].Value)).Insert(0, emContent);
            }
            else
            {
                return ImmutableArray.Create<IMarkdownToken>(emContent);
            }
        }
    }
}
