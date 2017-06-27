// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public abstract class MarkdownLinkBaseInlineRule : IMarkdownRule
    {
        private static readonly object BoxedTrue = true;

        public abstract string Name { get; }

        public abstract IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context);

        protected virtual IMarkdownToken GenerateToken(IMarkdownParser parser, string href, string title, string text, bool isImage, SourceInfo sourceInfo, MarkdownLinkType linkType, string refId)
        {
            var c = parser.SwitchContext(MarkdownInlineContext.IsInLink, BoxedTrue);
            IMarkdownToken result;
            if (isImage)
            {
                if (parser.Options.LegacyMode)
                {
                    result = new MarkdownImageInlineToken(
                        this,
                        parser.Context,
                        StringHelper.LegacyUnescapeMarkdown(href),
                        StringHelper.LegacyUnescapeMarkdown(title),
                        StringHelper.LegacyUnescapeMarkdown(text),
                        sourceInfo,
                        linkType,
                        refId);
                }
                else
                {
                    result = new MarkdownImageInlineToken(
                        this,
                        parser.Context,
                        StringHelper.UnescapeMarkdown(href),
                        StringHelper.UnescapeMarkdown(title),
                        StringHelper.UnescapeMarkdown(text),
                        sourceInfo,
                        linkType,
                        refId);
                }
            }
            else
            {
                if (parser.Options.LegacyMode)
                {
                    result = new MarkdownLinkInlineToken(
                        this,
                        parser.Context,
                        StringHelper.LegacyUnescapeMarkdown(href),
                        StringHelper.LegacyUnescapeMarkdown(title),
                        parser.Tokenize(sourceInfo.Copy(text)),
                        sourceInfo,
                        linkType,
                        refId);
                }
                else
                {
                    result = new MarkdownLinkInlineToken(
                        this,
                        parser.Context,
                        StringHelper.UnescapeMarkdown(href),
                        StringHelper.UnescapeMarkdown(title),
                        parser.Tokenize(sourceInfo.Copy(text)),
                        sourceInfo,
                        linkType,
                        refId);
                }
            }
            parser.SwitchContext(c);
            return result;
        }
    }
}
