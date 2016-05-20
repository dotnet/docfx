// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownHeadingBlockRule : IMarkdownRule
    {
        public string Name => "Heading";

        public virtual Regex Heading => Regexes.Block.Heading;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Heading.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo = context.LineInfo;
            context.Consume(match.Length);
            return new TwoPhaseBlockToken(
                this,
                parser.Context,
                match.Value,
                lineInfo,
                (p, t) => new MarkdownHeadingBlockToken(
                    t.Rule,
                    t.Context,
                    p.TokenizeInline(match.Groups[2].Value, t.LineInfo),
                    Regex.Replace(match.Groups[2].Value.ToLower(), @"[^\p{L}\p{N}]+", "-"),
                    match.Groups[1].Value.Length,
                    t.RawMarkdown,
                    t.LineInfo));
        }
    }
}
