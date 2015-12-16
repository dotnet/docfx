// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public abstract class MarkdownLinkBaseInlineRule : IMarkdownRule
    {
        public abstract string Name { get; }

        public abstract IMarkdownToken TryMatch(MarkdownEngine engine, ref string source);

        protected virtual IMarkdownToken GenerateToken(MarkdownEngine engine, string href, string title, string text, bool isImage)
        {
            var escapedHref = StringHelper.Escape(href);
            var escapedTitle = !string.IsNullOrEmpty(title) ? StringHelper.Escape(title) : null;
            if (isImage)
            {
                return new MarkdownImageInlineToken(this, engine.Context, escapedHref, escapedTitle, text);
            }
            else
            {
                return new MarkdownLinkInlineToken(this, engine.Context, escapedHref, escapedTitle, engine.Tokenize(text));
            }
        }
    }
}
