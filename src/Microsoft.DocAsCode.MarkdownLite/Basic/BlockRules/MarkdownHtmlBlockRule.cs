// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    public class MarkdownHtmlBlockRule : IMarkdownRule
    {
        public string Name => "Html";

        public virtual Regex Html => Regexes.Block.Html;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Html.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            bool isPre = engine.Options.Sanitizer == null &&
                (match.Groups[1].Value == "pre" || match.Groups[1].Value == "script" || match.Groups[1].Value == "style");
            if (engine.Options.Sanitize)
            {
                return new TwoPhaseBlockToken(this, engine.Context, match.Value, (p, t) =>
                    new MarkdownParagraphBlockToken(t.Rule, t.Context, p.TokenizeInline(match.Value), t.RawMarkdown));
            }
            else
            {
                return new TwoPhaseBlockToken(this, engine.Context, match.Value, (p, t) =>
                    new MarkdownHtmlBlockToken(
                        t.Rule,
                        t.Context,
                        isPre ? new InlineContent(new IMarkdownToken[] { new MarkdownRawToken(this, engine.Context, t.RawMarkdown) }.ToImmutableArray()) : p.TokenizeInline(t.RawMarkdown),
                        t.RawMarkdown));
            }
        }
    }
}
