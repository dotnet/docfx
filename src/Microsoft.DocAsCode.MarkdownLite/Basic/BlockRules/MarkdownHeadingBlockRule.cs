// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownHeadingBlockRule : IMarkdownRule
    {
        public string Name => "Heading";

        public virtual Regex Heading => Regexes.Block.Heading;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Heading.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new TwoPhaseBlockToken(this, engine.Context, match.Value, (p, t) =>
                new MarkdownHeadingBlockToken(
                    t.Rule,
                    t.Context,
                    p.TokenizeInline(match.Groups[2].Value),
                    Regex.Replace(match.Groups[2].Value.ToLower(), @"[^\w]+", "-"),
                    match.Groups[1].Value.Length,
                    t.RawMarkdown));
        }
    }
}
