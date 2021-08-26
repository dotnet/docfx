// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using HtmlReaderWriter;
using Markdig.Syntax;

namespace Microsoft.Docs.Build
{
    internal class HtmlSanitizer
    {
        private readonly Dictionary<string, HashSet<string>?> s_allowedTags;

        private readonly HashSet<string> s_allowedGlobalAttributes;

        public HtmlSanitizer()
        {
        }

        public void SanitizeHtml(ErrorBuilder errors, ref HtmlReader reader, ref HtmlToken token, MarkdownObject? obj)
        {
            if (token.Type != HtmlTokenType.StartTag)
            {
                return;
            }

            var tokenName = token.Name.ToString();
            if (!s_allowedTags.TryGetValue(tokenName, out var allowedAttributes))
            {
                errors.Add(Errors.Content.DisallowedHtmlTag(obj?.GetSourceInfo()?.WithOffset(token.NameRange), tokenName));
                reader.ReadToEndTag(token.Name.Span);
                token = default;
                return;
            }

            foreach (ref var attribute in token.Attributes.Span)
            {
                var attributeName = attribute.Name.ToString();
                if (!IsAllowedAttribute(attributeName))
                {
                    errors.Add(Errors.Content.DisallowedHtmlAttribute(obj?.GetSourceInfo()?.WithOffset(attribute.NameRange), tokenName, attributeName));
                    attribute = default;
                }
            }

            bool IsAllowedAttribute(string attributeName)
            {
                if (s_allowedGlobalAttributes.Contains(attributeName))
                {
                    return true;
                }

                if (allowedAttributes != null && allowedAttributes.Contains(attributeName))
                {
                    return true;
                }

                if (attributeName.StartsWith("aria-", StringComparison.OrdinalIgnoreCase) ||
                    attributeName.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
