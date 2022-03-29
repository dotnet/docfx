// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using HtmlReaderWriter;
using Markdig.Syntax;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal static class HtmlUtility
{
    public delegate void TransformHtmlDelegate(ref HtmlReader reader, ref HtmlWriter writer, ref HtmlToken token);

    private static readonly string[] s_htmlMetaHidden = new[]
    {
        "titleSuffix",
        "contributors_to_exclude",
        "helpviewer_keywords",
        "dev_langs",
        "f1_keywords",
        "api_scan",
        "layout",
        "open_to_public_contributors",
        "title",
        "absolutePath",
        "original_content_git_url_template",
        "fileRelativePath",
        "internal_document_id",
        "product_family",
        "product_version",
        "redirect_url",
        "redirect_document_id",
        "toc_asset_id",
        "content_git_url",
        "area",
        "theme",
        "theme_branch",
        "theme_url",
        "is_active",
        "publish_version",
        "canonical_url",
        "is_dynamic_rendering",
        "need_preview_pull_request",
        "moniker_type",
        "is_significant_update",
        "serviceData",
        "github_contributors",
        "is_hidden",
        "cmProducts",
    };

    private static readonly Dictionary<string, string> s_htmlMetaNames = new()
    {
        { "product", "Product" },
        { "topic_type", "TopicType" },
        { "api_type", "APIType" },
        { "api_location", "APILocation" },
        { "api_name", "APIName" },
        { "api_extra_info", "APIExtraInfo" },
        { "target_os", "TargetOS" },
    };

    private static readonly string[] s_inlineTags = new[]
    {
        "a", "area", "del", "ins", "link", "map", "meta", "abbr", "audio", "b", "bdo", "button", "canvas", "cite", "code", "command", "data",
        "datalist", "dfn", "em", "embed", "i", "iframe", "img", "input", "kbd", "keygen", "label", "mark", "math", "meter", "noscript", "object",
        "output", "picture", "progress", "q", "ruby", "samp", "script", "select", "small", "span", "strong", "sub", "sup", "svg", "textarea", "time",
        "var", "video", "wbr",
    };

    private static readonly string[] s_selfClosingTags = new[]
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

    public static void TransformLink(ref HtmlToken token, MarkdownObject? block, Func<LinkInfo, string> transformLink)
    {
        foreach (ref var attribute in token.Attributes.Span)
        {
            if (IsLink(ref token, attribute, out var tagName, out var attributeName))
            {
                var href = new SourceInfo<string>(
                    HttpUtility.HtmlDecode(attribute.Value.ToString()),
                    block?.GetSourceInfo()?.WithOffset(attribute.ValueRange));

                var link = transformLink(new()
                {
                    Href = href,
                    MarkdownObject = block,
                    TagName = tagName,
                    AttributeName = attributeName,
                    AltText = token.GetAttributeValueByName("alt"),
                    HtmlSourceIndex = token.Range.Start.Index,
                });

                attribute = attribute.WithValue(HttpUtility.HtmlEncode(link));
            }
        }
    }

    public static void TransformXref(
        ref HtmlReader reader,
        ref HtmlToken token,
        MarkdownObject? block,
        Func<SourceInfo<string>?, SourceInfo<string>?, bool, XrefLink> resolveXref)
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
                suppressXrefNotFound = bool.TryParse(attribute.Value.Span, out var warn) && !warn;
            }
        }

        suppressXrefNotFound = suppressXrefNotFound || ((rawHtml ?? rawSource)?.StartsWith("@") ?? false);

        var xrefLink = resolveXref(
            href == null ? null : new SourceInfo<string>(href, block?.GetSourceInfo()?.WithOffset(token.Range)),
            uid == null ? null : new SourceInfo<string>(uid, block?.GetSourceInfo()?.WithOffset(token.Range)),
            suppressXrefNotFound);

        var resolvedNode = string.IsNullOrEmpty(xrefLink.Href)
            ? rawHtml is not null
                ? AddNoLocSpan(rawHtml)
                : rawSource is not null ? AddNoLocSpan(rawSource) : GetDefaultResolvedNode()
            : StringUtility.Html($"<a {(xrefLink.Localizable ? string.Empty : "class=no-loc ")}href='{xrefLink.Href}'>{xrefLink.Display}</a>");

        token = new HtmlToken(resolvedNode);

        string GetDefaultResolvedNode()
        {
            var content = !string.IsNullOrEmpty(xrefLink.Display) ?
                xrefLink.Display : (href != null ? UrlUtility.SplitUrl(href).path : uid);
            return StringUtility.Html($"<span class=\"{(xrefLink.Localizable ? string.Empty : "no-loc ")}xref\">{content}</span>");
        }

        static string AddNoLocSpan(string? content)
            => $"<span class=\"no-loc\">{content}</span>";
    }

    public static string CreateHtmlMetaTags(JObject metadata)
    {
        var result = new StringBuilder();

        foreach (var (key, value) in ((IEnumerable<KeyValuePair<string, JToken>>)metadata).OrderBy(p => p.Key))
        {
            if (value is null || value is JObject || s_htmlMetaHidden.Contains(key))
            {
                continue;
            }

            var content = "";
            var name = s_htmlMetaNames.TryGetValue(key, out var displayName) ? displayName : key;

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
            else
            {
                content = value.Type == JTokenType.Boolean ? (bool)value ? "true" : "false" : value.ToString();
            }

            result.AppendLine($"<meta name=\"{Encode(name)}\" content=\"{Encode(content)}\" />");
        }

        return result.ToString();
    }

    public static void CollectHtmlUsage(string html, Dictionary<string, Dictionary<string, int>> elementCount)
    {
        var reader = new HtmlReader(html);

        while (reader.Read(out var token))
        {
            if (token.Type != HtmlTokenType.StartTag)
            {
                continue;
            }

            var tokenName = token.Name.ToString().ToLowerInvariant();
            if (!elementCount.ContainsKey(tokenName))
            {
                elementCount.Add(tokenName, new Dictionary<string, int>());
            }

            var attributeCount = elementCount[tokenName];
            if (token.Attributes.Span.IsEmpty)
            {
                CollectionsMarshal.GetValueRefOrAddDefault(attributeCount, string.Empty, out _)++;
            }
            else
            {
                foreach (ref var attribute in token.Attributes.Span)
                {
                    var attributeName = attribute.Name.ToString().ToLowerInvariant();
                    CollectionsMarshal.GetValueRefOrAddDefault(attributeCount, attributeName, out _)++;
                }
            }
        }
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

    internal static void AddLinkType(ref HtmlToken token)
    {
        foreach (ref readonly var attribute in token.Attributes.Span)
        {
            if (attribute.Value.Length > 0 && IsLink(ref token, attribute, out _, out _))
            {
                var href = attribute.Value.ToString();

                switch (UrlUtility.GetLinkType(href))
                {
                    case LinkType.SelfBookmark:
                        token.SetAttributeValue("data-linktype", "self-bookmark");
                        break;
                    case LinkType.AbsolutePath:
                        token.SetAttributeValue("data-linktype", "absolute-path");
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
    }

    internal static void AddLocaleIfMissingForAbsolutePath(ref HtmlToken token, string locale)
    {
        foreach (ref readonly var attribute in token.Attributes.Span)
        {
            if (attribute.Value.Length > 0 && IsLink(ref token, attribute, out _, out _))
            {
                var href = attribute.Value.ToString();

                if (UrlUtility.GetLinkType(href) == LinkType.AbsolutePath)
                {
                    token.SetAttributeValue(attribute.Name.ToString(), AddLocaleIfMissingForAbsolutePath(href, locale));
                }
            }
        }
    }

    private static string AddLocaleIfMissingForAbsolutePath(string href, string locale)
    {
        // should not add locale for api, _* links
        if (href.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("/_", StringComparison.OrdinalIgnoreCase))
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

    private static bool IsLink(
        ref HtmlToken token, in HtmlAttribute attribute, [NotNullWhen(true)] out string? tagName, [NotNullWhen(true)] out string? attributeName)
    {
        if (token.NameIs("a") && attribute.NameIs("href"))
        {
            tagName = "a";
            attributeName = "href";
            return true;
        }

        if (attribute.NameIs("src") || attribute.NameIs("poster"))
        {
            tagName = token.NameIs("image")
                ? "img"
                : token.Name.ToString().Trim().ToLowerInvariant();
            attributeName = attribute.Name.ToString().Trim().ToLowerInvariant();
            return true;
        }

        tagName = attributeName = null;
        return false;
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
}
