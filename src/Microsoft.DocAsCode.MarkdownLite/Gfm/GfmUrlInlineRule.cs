// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    public class GfmUrlInlineRule : IMarkdownRule
    {
        public virtual string Name => "Inline.Gfm.Url";

        public virtual Regex Url => Regexes.Inline.Gfm.Url;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (MarkdownInlineContext.GetIsInLink(parser.Context))
            {
                return null;
            }
            var match = Url.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var text = StringHelper.Escape(match.Groups[1].Value);
            if (!Uri.IsWellFormedUriString(text, UriKind.RelativeOrAbsolute))
            {
                return null;
            }

            var sourceInfo = context.Consume(match.Length);
            return new MarkdownLinkInlineToken(
                this,
                parser.Context,
                text,
                null,
                ImmutableArray.Create<IMarkdownToken>(
                    new MarkdownRawToken(this, parser.Context, sourceInfo.Copy(match.Groups[1].Value))),
                sourceInfo,
                MarkdownLinkType.UrlLink,
                null);
        }
    }
}
