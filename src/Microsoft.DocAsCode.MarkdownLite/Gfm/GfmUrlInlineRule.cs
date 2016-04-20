// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    public class GfmUrlInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Gfm.Url";

        public virtual Regex Url => Regexes.Inline.Gfm.Url;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, ref string source)
        {
            if ((bool)parser.Context.Variables[MarkdownInlineContext.IsInLink])
            {
                return null;
            }
            var match = Url.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            var text = StringHelper.Escape(match.Groups[1].Value);
            if (!Uri.IsWellFormedUriString(text, UriKind.RelativeOrAbsolute))
            {
                return null;
            }

            source = source.Substring(match.Length);
            return new MarkdownLinkInlineToken(
                this,
                parser.Context,
                text,
                null,
                ImmutableArray<IMarkdownToken>.Empty.Add(
                    new MarkdownRawToken(this, parser.Context, text)),
                match.Value);
        }
    }
}
