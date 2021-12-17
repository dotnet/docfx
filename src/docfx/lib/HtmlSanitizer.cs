// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using HtmlReaderWriter;
using Markdig.Syntax;

namespace Microsoft.Docs.Build;

internal class HtmlSanitizer
{
    public static readonly Dictionary<string, HashSet<string>?> DefaultAllowedHtml = new()
    {
        {
            "*",
            new()
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
        { "a", new() { "href", "target", "rel", "alt", "download", "tabindex" } },
        { "abbr", null },
        { "address", null },
        { "article", null },
        { "b", null },
        { "button", new() { "hidden", "type" } },
        { "bdi", null },
        { "bdo", null },
        { "blockquote", new() { "cite" } },
        { "br", new() { "clear" } },
        { "caption", null },
        { "center", null },
        { "cite", null },
        { "code", new() { "name", "lang" } },
        { "col", new() { "width", "span" } },
        { "colgroup", new() { "span" } },
        { "dd", null },
        { "del", new() { "cite", "datetime" } },
        { "details", null },
        { "dfn", null },
        { "div", new() { "align", "hidden" } },
        { "dl", null },
        { "dt", null },
        { "em", null },
        { "figcaption", null },
        { "figure", null },
        { "font", new() { "color", "face", "size" } },
        { "form", new() { "action" } },
        { "h1", null },
        { "h2", null },
        { "h3", null },
        { "h4", null },
        { "head", null },
        { "hr", new() { "size", "color", "width" } },
        { "i", null },
        {
            "iframe",
            new()
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
        { "image", new() { "alt", "height", "src", "width" } },
        { "img", new() { "alt", "height", "src", "width", "align", "hspace", "border", "sizes", "valign" } },
        { "input", new() { "type", "value" } },
        { "ins", new() { "cite", "datetime" } },
        { "kbd", null },
        { "label", new() { "for" } },
        { "li", new() { "value" } },
        { "mark", null },
        { "nav", null },
        { "nobr", null },
        { "ol", new() { "reserved", "start", "type" } },
        { "p", new() { "align", "dir", "hidden", "lang", "valign" } },
        { "pre", new() { "lang" } },
        { "q", new() { "cite" } },
        { "rgn", null },
        { "s", null },
        { "samp", null },
        { "section", null },
        { "small", null },
        { "source", new() { "src", "type" } },
        { "span", new() { "dir", "lang" } },
        { "strike", null },
        { "strong", null },
        { "sub", null },
        { "summary", null },
        { "sup", null },
        {
            "table",
            new()
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
        { "tbody", new() { "align", "valign", "width" } },
        { "td", new() { "rowspan", "colspan", "align", "width", "valign", "bgcolor", "hidden", "nowrap" } },
        { "tfoot", null },
        { "th", new() { "rowspan", "colspan", "align", "width", "bgcolor", "scope", "valign" } },
        { "thead", new() { "align", "valign" } },
        { "time", new() { "datetime" } },
        { "tr", new() { "align", "valign", "colspan", "height", "bgcolor" } },
        { "u", null },
        { "ul", null },
        { "var", null },
        { "video", new() { "src", "width", "height", "preload", "controls", "poster" } },
        { "wbr", null },
    };

    private readonly Dictionary<string, HashSet<string>?> _allowedHtml;

    public HtmlSanitizer(Config config)
    {
        _allowedHtml = config.AllowedHtml.ToDictionary(
            i => i.Key,
            i => i.Value != null ? new HashSet<string>(i.Value, StringComparer.OrdinalIgnoreCase) : null,
            StringComparer.OrdinalIgnoreCase);
    }

    public void SanitizeHtml(ErrorBuilder errors, ref HtmlReader reader, ref HtmlToken token, MarkdownObject? obj)
    {
        if (token.Type != HtmlTokenType.StartTag)
        {
            return;
        }

        var tokenName = token.Name.ToString();
        if (!_allowedHtml.TryGetValue(tokenName, out var allowedAttributes))
        {
            errors.Add(Errors.Content.DisallowedHtmlTag(obj?.GetSourceInfo()?.WithOffset(token.NameRange), tokenName));
            reader.ReadToEndTag(token.Name.Span);
            token = default;
            return;
        }

        foreach (ref var attribute in token.Attributes.Span)
        {
            var attributeName = attribute.Name.ToString();
            if (!IsAllowedAttribute(attributeName, allowedAttributes))
            {
                errors.Add(Errors.Content.DisallowedHtmlAttribute(obj?.GetSourceInfo()?.WithOffset(attribute.NameRange), tokenName, attributeName));
                attribute = default;
            }
        }
    }

    public bool IsAllowedHtml(string tokenName, string attributeName)
    {
        if (!_allowedHtml.TryGetValue(tokenName, out var allowedAttributes))
        {
            return false;
        }

        return string.IsNullOrEmpty(attributeName) || IsAllowedAttribute(attributeName, allowedAttributes);
    }

    private bool IsAllowedAttribute(string attributeName, HashSet<string>? allowedAttributes)
    {
        if (_allowedHtml.TryGetValue("*", out var allowedGlobalAttributes)
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
