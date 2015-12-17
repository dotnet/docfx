// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownTagInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Tag";

        public virtual Regex Tag => Regexes.Inline.Tag;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Tag.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            var context = engine.Context;
            var inLink = (bool)context.Variables[MarkdownInlineContext.IsInLink];
            if (!inLink && Regexes.Lexers.StartHtmlLink.IsMatch(match.Value))
            {
                engine.SwitchContext(MarkdownInlineContext.IsInLink, true);
            }
            else if (inLink && Regexes.Lexers.EndHtmlLink.IsMatch(match.Value))
            {
                engine.SwitchContext(MarkdownInlineContext.IsInLink, false);
            }
            return new MarkdownTagInlineToken(this, engine.Context, match.Value);
        }
    }
}
