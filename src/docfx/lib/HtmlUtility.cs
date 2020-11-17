// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using HtmlReaderWriter;
using Markdig.Syntax;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class HtmlUtility
    {
        public delegate void TransformHtmlDelegate(ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token);

        private static readonly HashSet<string> s_globalAllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "name", "id", "class", "itemid", "itemprop", "itemref", "itemscope", "itemtype", "part", "slot", "spellcheck", "title", "role",
        };

        // ref https://developer.mozilla.org/en-US/docs/Web/HTML/Element
        private static readonly Dictionary<string, HashSet<string>?> s_allowedTagAttributeMap =
            new Dictionary<string, HashSet<string>?>(StringComparer.OrdinalIgnoreCase)
        {
            // Content sectioning
            { "address", null },
            { "section", null },

            // Text content
            { "blockquote", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cite" } },
            { "dd", null },
            { "div", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "align" } },
            { "dl", null },
            { "dt", null },
            { "figcaption", null },
            { "figure", null },
            { "hr", null },
            { "li", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "value" } },
            { "ol", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "reversed", "start", "type" } },
            { "p", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "align" } },
            { "pre", null },
            { "ul", null },

            // Inline text semantics
            { "a", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "target", "rel" } },
            { "abbr", null },
            { "b", null },
            { "bdi", null },
            { "bdo", null },
            { "br", null },
            { "cite", null },
            { "code", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "name" } },
            { "dfn", null },
            { "em", null },
            { "i", null },
            { "kbd", null },
            { "mark", null },
            { "q", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cite" } },
            { "s", null },
            { "samp", null },
            { "small", null },
            { "span", null },
            { "strong", null },
            { "sub", null },
            { "sup", null },
            { "time", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "datetime" } },
            { "u", null },
            { "var", null },
            { "wbr", null },

            // Image and multimedia
            { "img", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alt", "height", "src", "width", "align" } },
            { "image", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alt", "height", "src", "width" } },

            // Demarcating edits
            { "del", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cite", "datetime" } },
            { "ins", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cite", "datetime" } },

            // table
            { "caption", null },
            { "col", null },
            { "colgroup", null },
            { "table", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "align", "width", "border" } },
            { "tbody", null },
            { "td", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rowspan", "colspan", "align", "width" } },
            { "tfoot", null },
            { "th", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rowspan", "colspan", "align", "width" } },
            { "thead", null },
            { "tr", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "align" } },

            // other
            {
              "iframe", new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        { "frameborder", "allowtransparency", "allowfullscreen", "scrolling", "height", "src", "width" }
            },
            { "center", null },
            { "summary", null },
            { "details", null },
            { "nobr", null },
            { "strike", null },
        };

        private static string[] s_inlineTags = new[]
            {
                "a", "area", "del", "ins", "link", "map", "meta", "abbr", "audio", "b", "bdo", "button", "canvas", "cite", "code", "command", "data",
                "datalist", "dfn", "em", "embed", "i", "iframe", "img", "input", "kbd", "keygen", "label", "mark", "math", "meter", "noscript", "object",
                "output", "picture", "progress", "q", "ruby", "samp", "script", "select", "small", "span", "strong", "sub", "sup", "svg", "textarea", "time",
                "var", "video", "wbr",
            };

        private static string[] s_selfClosingTags = new[]
            {
                "area", "base", "br", "col", "command", "embed", "hr", "img", "input", "link", "meta", "param", "source",
            };

        public static string TransformHtml(string html, TransformHtmlDelegate transform)
        {
            var result = new ArrayBufferWriter<char>(html.Length + 64);
            var reader = new HtmlReader(html);
            var writer = new HtmlWriter(result);

            while (reader.Read(out var token))
            {
                transform(ref reader, ref writer, ref token);
                writer.Write(ref token);
            }

            return result.WrittenSpan.ToString();
        }

        public static void CountWord(ref HtmlToken token, ref long wordCount)
        {
            if (token.Type == HtmlTokenType.Text)
            {
                wordCount += CountWordInText(token.RawText.Span);
            }
        }

        public static void GetBookmarks(ref HtmlToken token, HashSet<string> bookmarks)
        {
            foreach (ref readonly var attribute in token.Attributes.Span)
            {
                if ((attribute.NameIs("id") || attribute.NameIs("name")) && attribute.Value.Length > 0)
                {
                    bookmarks.Add(attribute.Value.ToString());
                }
            }
        }

        public static bool IsVisible(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return false;
            }

            var reader = new HtmlReader(html);
            while (reader.Read(out var token))
            {
                if (IsVisible(ref token))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsInlineImage(this HtmlBlock node, int imageIndex)
        {
            var stack = new Stack<(HtmlToken? token, int visibleInlineCount, bool hasImage)>();
            stack.Push((null, 0, false));
            var reader = new HtmlReader(node.Lines.ToString());
            while (reader.Read(out var token))
            {
                var top = stack.Pop();
                switch (token.Type)
                {
                    case HtmlTokenType.StartTag:
                        if (token.IsInlineElement())
                        {
                            top.visibleInlineCount += 1;

                            // Only look for the image specified by source info
                            if (token.NameIs("img") && token.Range.Start.Index == imageIndex)
                            {
                                top.hasImage = true;
                            }
                        }
                        else
                        {
                            if (top.hasImage && top.visibleInlineCount > 1)
                            {
                                return true;
                            }

                            top.visibleInlineCount = 0;
                            top.hasImage = false;
                        }
                        stack.Push(top);
                        if (!token.IsSelfClosing && !token.IsSelfClosingElement())
                        {
                            stack.Push((token, 0, false));
                        }
                        break;
                    case HtmlTokenType.EndTag:
                        if (!top.token.HasValue || !top.token.Value.NameIs(token.Name.Span))
                        {
                            // Invalid HTML structure, should throw warning
                            stack.Push(top);
                        }
                        else
                        {
                            if (top.hasImage)
                            {
                                if (top.visibleInlineCount > 1)
                                {
                                    return true;
                                }
                                if (top.token.Value.IsInlineElement())
                                {
                                    var parent = stack.Pop();
                                    parent.hasImage = true;
                                    stack.Push(parent);
                                }
                            }
                        }
                        break;
                    default:
                        if (IsVisible(ref token))
                        {
                            top.visibleInlineCount++;
                        }
                        stack.Push(top);
                        break;
                }
            }

            // Should check if all tags are closed properly and throw warning if not
            return false;
        }

        public static void TransformLink(
            ref HtmlToken token,
            MarkdownObject? block,
            Func<SourceInfo<string>, string> transformLink,
            Func<SourceInfo<string>, MarkdownObject?, string?, int, string>? transformImageLink = null)
        {
            foreach (ref var attribute in token.Attributes.Span)
            {
                if (IsLink(ref token, attribute))
                {
                    var source = block?.GetSourceInfo()?.WithOffset(attribute.ValueRange);
                    var link = HttpUtility.HtmlEncode(
                        !IsImage(ref token, attribute) || transformImageLink == null
                            ? transformLink(new SourceInfo<string>(HttpUtility.HtmlDecode(attribute.Value.ToString()), source))
                            : transformImageLink(
                                new SourceInfo<string>(HttpUtility.HtmlDecode(attribute.Value.ToString()), source),
                                block,
                                token.GetAttributeValueByName("alt"),
                                token.Range.Start.Index));

                    attribute = attribute.WithValue(link);
                }
            }
        }

        public static void TransformXref(
            ref HtmlReader reader,
            ref HtmlToken token,
            MarkdownObject? block,
            Func<SourceInfo<string>?, SourceInfo<string>?, bool, (string? href, string display)> resolveXref)
        {
            if (!token.NameIs("xref"))
            {
                return;
            }

            if (token.Type != HtmlTokenType.StartTag)
            {
                token = default;
                return;
            }

            reader.ReadToEndTag(token.Name.Span);

            var rawHtml = default(string);
            var rawSource = default(string);
            var href = default(string);
            var uid = default(string);
            var suppressXrefNotFound = false;

            foreach (ref readonly var attribute in token.Attributes.Span)
            {
                if (attribute.NameIs("data-raw-html"))
                {
                    rawHtml = attribute.Value.ToString();
                }
                else if (attribute.NameIs("data-raw-source"))
                {
                    rawSource = attribute.Value.ToString();
                }
                else if (attribute.NameIs("href"))
                {
                    href = HttpUtility.HtmlDecode(attribute.Value.ToString());
                }
                else if (attribute.NameIs("uid"))
                {
                    uid = HttpUtility.HtmlDecode(attribute.Value.ToString());
                }
                else if (attribute.NameIs("data-throw-if-not-resolved"))
                {
                    suppressXrefNotFound = bool.TryParse(attribute.Value.Span, out var warn) ? !warn : false;
                }
            }

            suppressXrefNotFound = suppressXrefNotFound || ((rawHtml ?? rawSource)?.StartsWith("@") ?? false);

            var (resolvedHref, display) = resolveXref(
                href == null ? null : (SourceInfo<string>?)new SourceInfo<string>(href, block?.GetSourceInfo()?.WithOffset(token.Range)),
                uid == null ? null : (SourceInfo<string>?)new SourceInfo<string>(uid, block?.GetSourceInfo()?.WithOffset(token.Range)),
                suppressXrefNotFound);

            var resolvedNode = string.IsNullOrEmpty(resolvedHref)
                ? rawHtml ?? rawSource ?? GetDefaultResolvedNode()
                : StringUtility.Html($"<a href='{resolvedHref}'>{display}</a>");

            token = new HtmlToken(resolvedNode);

            string GetDefaultResolvedNode()
            {
                var content = !string.IsNullOrEmpty(display) ? display : (href != null ? UrlUtility.SplitUrl(href).path : uid);
                return StringUtility.Html($"<span class=\"xref\">{content}</span>");
            }
        }

        public static string CreateHtmlMetaTags(JObject metadata, ICollection<string> htmlMetaHidden, IReadOnlyDictionary<string, string> htmlMetaNames)
        {
            var result = new StringBuilder();

            foreach (var (key, value) in metadata)
            {
                if (value is null || value is JObject || htmlMetaHidden.Contains(key))
                {
                    continue;
                }

                var content = "";
                var name = htmlMetaNames.TryGetValue(key, out var displayName) ? displayName : key;

                if (value is JArray arr)
                {
                    foreach (var v in value)
                    {
                        if (v is JValue)
                        {
                            result.AppendLine($"<meta name=\"{Encode(name)}\" content=\"{Encode(v.ToString())}\" />");
                        }
                    }
                    continue;
                }
                else if (value.Type == JTokenType.Boolean)
                {
                    content = (bool)value ? "true" : "false";
                }
                else
                {
                    content = value.ToString();
                }

                result.AppendLine($"<meta name=\"{Encode(name)}\" content=\"{Encode(content)}\" />");
            }

            return result.ToString();
        }

        public static SourceInfo WithOffset(this SourceInfo sourceInfo, in HtmlTextRange range)
        {
            return sourceInfo.WithOffset(range.Start.Line + 1, range.Start.Column + 1, range.End.Line + 1, range.End.Column + 1);
        }

        /// <summary>
        /// Special HTML encode logic designed only for <see cref="CreateHtmlMetaTags"/>.
        /// </summary>
        internal static string Encode(string s)
        {
            return s.Replace("&", "&amp;")
                    .Replace("\"", "&quot;");
        }

        internal static void RemoveRerunCodepenIframes(ref HtmlToken token)
        {
            // the rerun button on codepen iframes isn't accessible.
            // rather than get acc bugs or ban codepen, we're just hiding the rerun button using their iframe api
            if (token.NameIs("iframe"))
            {
                foreach (ref var attribute in token.Attributes.Span)
                {
                    if (attribute.NameIs("src") && attribute.Value.Span.Contains("codepen.io", StringComparison.OrdinalIgnoreCase))
                    {
                        attribute = attribute.WithValue(attribute.Value.ToString() + "&rerun-position=hidden&");
                    }
                }
            }
        }

        internal static void StripTags(ref HtmlReader reader, ref HtmlToken token)
        {
            if (token.NameIs("script") || token.NameIs("link") || token.NameIs("style"))
            {
                reader.ReadToEndTag(token.Name.Span);
                token = default;
                return;
            }

            foreach (ref var attribute in token.Attributes.Span)
            {
                if (attribute.NameIs("style") || attribute.Name.Span.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                {
                    attribute = default;
                }
            }
        }

        internal static void ScanTags(ref HtmlToken token, MarkdownObject obj, ErrorBuilder errors)
        {
            if (token.Type != HtmlTokenType.StartTag)
            {
                return;
            }

            var tokenName = token.Name.ToString();
            if (!s_allowedTagAttributeMap.TryGetValue(tokenName, out var additionalAttributes))
            {
                errors.Add(Errors.Content.DisallowedHtml(obj.GetSourceInfo()?.WithOffset(token.NameRange), tokenName));
                return;
            }

            foreach (ref readonly var attribute in token.Attributes.Span)
            {
                var attributeName = attribute.Name.ToString();
                if (attribute.Name.Span.StartsWith("data-", StringComparison.OrdinalIgnoreCase) ||
                    attribute.Name.Span.StartsWith("aria-", StringComparison.OrdinalIgnoreCase) ||
                    s_globalAllowedAttributes.Contains(attributeName))
                {
                    continue;
                }

                if (additionalAttributes is null || !additionalAttributes.Contains(attributeName))
                {
                    errors.Add(Errors.Content.DisallowedHtml(obj.GetSourceInfo()?.WithOffset(attribute.NameRange), tokenName, attributeName));
                }
            }
        }

        internal static void AddLinkType(ref HtmlToken token, string locale)
        {
            foreach (ref readonly var attribute in token.Attributes.Span)
            {
                if (attribute.Value.Length <= 0 || !IsLink(ref token, attribute))
                {
                    continue;
                }

                var href = attribute.Value.ToString();

                switch (UrlUtility.GetLinkType(href))
                {
                    case LinkType.SelfBookmark:
                        token.SetAttributeValue("data-linktype", "self-bookmark");
                        break;
                    case LinkType.AbsolutePath:
                        token.SetAttributeValue("data-linktype", "absolute-path");
                        token.SetAttributeValue(attribute.Name.ToString(), AddLocaleIfMissingForAbsolutePath(href, locale));
                        break;
                    case LinkType.RelativePath:
                        token.SetAttributeValue("data-linktype", "relative-path");
                        break;
                    case LinkType.External:
                        token.SetAttributeValue("data-linktype", "external");
                        break;
                }
            }
        }

        private static string AddLocaleIfMissingForAbsolutePath(string href, string locale)
        {
            // should not add locale for api links
            if (href.StartsWith("/api"))
            {
                return href;
            }

            var pos = href.IndexOfAny(new[] { '/', '\\' }, 1);
            if (pos >= 1)
            {
                if (LocalizationUtility.IsValidLocale(href[1..pos]))
                {
                    return href;
                }
            }

            return $"/{locale}{href}";
        }

        private static bool IsLink(ref HtmlToken token, in HtmlAttribute attribute)
        {
            return (token.NameIs("a") && attribute.NameIs("href")) || IsImage(ref token, attribute);
        }

        private static bool IsImage(ref HtmlToken token, in HtmlAttribute attribute)
        {
            return token.NameIs("img") && attribute.NameIs("src") && !attribute.Value.IsEmpty;
        }

        private static bool IsVisible(ref HtmlToken token) => token.Type switch
        {
            HtmlTokenType.Text => !token.RawText.Span.IsWhiteSpace(),
            HtmlTokenType.Comment => false,
            _ => true,
        };

        private static bool IsInlineElement(string tagName) => s_inlineTags.Contains(tagName.ToLowerInvariant());

        private static bool IsInlineElement(this HtmlToken token) => IsInlineElement(token.Name.ToString());

        private static bool IsSelfClosingElement(string tagName) => s_selfClosingTags.Contains(tagName.ToLowerInvariant());

        private static bool IsSelfClosingElement(this HtmlToken token) => IsSelfClosingElement(token.Name.ToString());

        private static int CountWordInText(ReadOnlySpan<char> text)
        {
            var total = 0;
            var word = false;

            foreach (var ch in text)
            {
                if (IsCJKChar(ch))
                {
                    total++;

                    if (word)
                    {
                        word = false;
                        total++;
                    }
                }
                else
                {
                    if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                    {
                        if (word)
                        {
                            word = false;
                            total++;
                        }
                    }
                    else if (
                        ch != '.' && ch != '?' && ch != '!' &&
                        ch != ';' && ch != ':' && ch != ',' &&
                        ch != '(' && ch != ')' && ch != '[' &&
                        ch != ']')
                    {
                        word = true;
                    }
                }
            }

            if (word)
            {
                total++;
            }

            return total;
        }

        private static bool IsCJKChar(char ch)
        {
            return (ch >= '\u2E80' && ch <= '\u9FFF') || // CJK character
                   (ch >= '\xAC00' && ch <= '\xD7A3') || // Hangul Syllables
                   (ch >= '\uFF00' && ch <= '\uFFEF');   // Half width and Full width Forms (including Chinese punctuation)
        }
    }
}
