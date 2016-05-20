// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownLHeadingBlockRule : IMarkdownRule
    {
        public string Name => "LHeading";

        public virtual Regex LHeading => Regexes.Block.LHeading;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParserContext context)
        {
            var match = LHeading.Match(context.CurrentMarkdown);
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
                    p.TokenizeInline(match.Groups[1].Value, lineInfo),
                    Regex.Replace(match.Groups[1].Value.ToLower(), @"[^\w]+", "-"),
                    match.Groups[2].Value == "=" ? 1 : 2,
                    t.RawMarkdown,
                    t.LineInfo));
        }
    }
}
