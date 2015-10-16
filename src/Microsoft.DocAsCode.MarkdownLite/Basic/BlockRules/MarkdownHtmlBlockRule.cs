// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownHtmlBlockRule : IMarkdownRule
    {
        public string Name => "Html";

        public virtual Regex Html => Regexes.Block.Html;

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
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
                return new MarkdownParagraphBlockToken(this, match.Value);
            }
            else
            {
                return new MarkdownHtmlBlockToken(this, match.Value, isPre);
            }
        }
    }
}
