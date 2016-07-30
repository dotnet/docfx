// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public abstract class MarkdownLinkBaseInlineRule : IMarkdownRule
    {
        public abstract string Name { get; }

        public abstract IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context);

        protected virtual IMarkdownToken GenerateToken(IMarkdownParser parser, string href, string title, string text, bool isImage, SourceInfo sourceInfo)
        {
            var escapedHref = Regexes.Helper.MarkdownEscape.Replace(href, m => m.Groups[1].Value);
            if (isImage)
            {
                return new MarkdownImageInlineToken(this, parser.Context, escapedHref, title, text, sourceInfo);
            }
            else
            {
                return new MarkdownLinkInlineToken(this, parser.Context, escapedHref, title, parser.Tokenize(sourceInfo.Copy(text)), sourceInfo);
            }
        }
    }
}
