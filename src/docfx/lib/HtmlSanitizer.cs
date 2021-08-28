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
        public static readonly Dictionary<string, HashSet<string>?> DefaultAllowedHTML = new(StringComparer.OrdinalIgnoreCase)
        {
            { "a", new(StringComparer.OrdinalIgnoreCase) { "href", "target", "rel", "alt", "download", "tabindex" } },
            { "abbr", null },
            { "address", null },
            { "article", null },
            { "b", null },
            { "button", new(StringComparer.OrdinalIgnoreCase) { "hidden", "type" } },
            { "bdi", null },
            { "bdo", null },
            { "blockquote", new(StringComparer.OrdinalIgnoreCase) { "cite" } },
            { "br", new(StringComparer.OrdinalIgnoreCase) { "clear" } },
            { "caption", null },
            { "center", null },
            { "cite", null },
            { "code", new(StringComparer.OrdinalIgnoreCase) { "name", "lang" } },
            { "col", new(StringComparer.OrdinalIgnoreCase) { "width", "span" } },
            { "colgroup", new(StringComparer.OrdinalIgnoreCase) { "span" } },
            { "dd", null },
            { "del", new(StringComparer.OrdinalIgnoreCase) { "cite", "datetime" } },
            { "details", null },
            { "dfn", null },
            { "div", new(StringComparer.OrdinalIgnoreCase) { "align", "hidden" } },
            { "dl", null },
            { "dt", null },
            { "em", null },
            { "figcaption", null },
            { "figure", null },
            { "font", new(StringComparer.OrdinalIgnoreCase) { "color", "face", "size" } },
            { "form", new(StringComparer.OrdinalIgnoreCase) { "action" } },
            { "h1", null },
            { "h2", null },
            { "h3", null },
            { "h4", null },
            { "head", null },
            { "hr", new(StringComparer.OrdinalIgnoreCase) { "size", "color", "width" } },
            { "i", null },
            {
                "iframe",
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "allow",
                    "align",
                    "border",
                    "marginwidth",
                    "frameborder",
                    "allowtransparency",
                    "allowfullscreen",
                    "scrolling",
                    "height",
                    "src",
                    "width",
                    "loading",
                }
            },
            { "image", new(StringComparer.OrdinalIgnoreCase) { "alt", "height", "src", "width" } },
            { "img", new(StringComparer.OrdinalIgnoreCase) { "alt", "height", "src", "width", "align", "hspace", "border", "sizes", "valign" } },
            { "input", new(StringComparer.OrdinalIgnoreCase) { "type", "value" } },
            { "ins", new(StringComparer.OrdinalIgnoreCase) { "cite", "datetime" } },
            { "kbd", null },
            { "label", new(StringComparer.OrdinalIgnoreCase) { "for" } },
            { "li", new(StringComparer.OrdinalIgnoreCase) { "value" } },
            { "mark", null },
            { "nav", null },
            { "nobr", null },
            { "ol", new(StringComparer.OrdinalIgnoreCase) { "reserved", "start", "type" } },
            { "p", new(StringComparer.OrdinalIgnoreCase) { "align", "dir", "hidden", "lang", "valign" } },
            { "pre", new(StringComparer.OrdinalIgnoreCase) { "lang" } },
            { "q", new(StringComparer.OrdinalIgnoreCase) { "cite" } },
            { "rgn", null },
            { "s", null },
            { "samp", null },
            { "section", null },
            { "small", null },
            { "source", new(StringComparer.OrdinalIgnoreCase) { "src", "type" } },
            { "span", new(StringComparer.OrdinalIgnoreCase) { "dir", "lang" } },
            { "strike", null },
            { "strong", null },
            { "sub", null },
            { "summary", null },
            { "sup", null },
            {
                "table",
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "align",
                    "width",
                    "border",
                    "valign",
                    "bgcolor",
                    "frame",
                    "cellpadding",
                    "cellspacing",
                    "bordercolor",
                }
            },
            { "tbody", new(StringComparer.OrdinalIgnoreCase) { "align", "valign", "width" } },
            { "td", new(StringComparer.OrdinalIgnoreCase) { "rowspan", "colspan", "align", "width", "valign", "bgcolor", "hidden", "nowrap" } },
            { "tfoot", null },
            { "th", new(StringComparer.OrdinalIgnoreCase) { "rowspan", "colspan", "align", "width", "bgcolor", "scope", "valign" } },
            { "thead", new(StringComparer.OrdinalIgnoreCase) { "align", "valign" } },
            { "time", new(StringComparer.OrdinalIgnoreCase) { "datetime" } },
            { "tr", new(StringComparer.OrdinalIgnoreCase) { "align", "valign", "colspan", "height", "bgcolor" } },
            { "u", null },
            { "ul", null },
            { "var", null },
            { "video", new(StringComparer.OrdinalIgnoreCase) { "src", "width", "height", "preload", "controls", "poster" } },
            { "wbr", null },
            {
                "Global Attributes",
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "name",
                    "id",
                    "class",
                    "itemid",
                    "itemprop",
                    "itemref",
                    "itemscope",
                    "itemtype",
                    "part",
                    "slot",
                    "spellcheck",
                    "title",
                    "role",
                }
            },
        };

        private readonly Config _config;

        public HtmlSanitizer(Config config)
        {
            _config = config;
        }

        public void SanitizeHtml(ErrorBuilder errors, ref HtmlReader reader, ref HtmlToken token, MarkdownObject? obj)
        {
            var allowedHTML = _config.AllowedHTML;
            if (token.Type != HtmlTokenType.StartTag)
            {
                return;
            }

            var tokenName = token.Name.ToString();
            if (!allowedHTML.TryGetValue(tokenName, out var allowedAttributes))
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
                if (allowedHTML.TryGetValue("Global Attributes", out var allowedGlobalAttributes)
                    && allowedGlobalAttributes != null
                    && allowedGlobalAttributes.Contains(attributeName))
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
