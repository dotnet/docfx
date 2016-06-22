// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownHeadingBlockRule : IMarkdownRule
    {
        public virtual string Name => "Heading";

        public virtual Regex Heading => Regexes.Block.Heading;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Heading.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new TwoPhaseBlockToken(
                this,
                parser.Context,
                sourceInfo,
                (p, t) => new MarkdownHeadingBlockToken(
                    t.Rule,
                    t.Context,
                    p.TokenizeInline(t.SourceInfo.Copy(match.Groups[2].Value)),
                    Regex.Replace(match.Groups[2].Value.ToLower(), @"[^\p{L}\p{N}]+", "-").Trim('-'),
                    match.Groups[1].Value.Length,
                    t.SourceInfo));
        }
    }
}
