// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using HtmlAgilityPack;

namespace Microsoft.Docs.Build
{
    internal static class HtmlUtility
    {
        private static readonly Func<HtmlAgilityPack.HtmlAttribute, int> s_getValueStartIndex =
            ReflectionUtility.CreateInstanceFieldGetter<HtmlAgilityPack.HtmlAttribute, int>("_valuestartindex");

        public static HtmlNode LoadHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode;
        }

        public static string TransformHtml(string html, Func<HtmlNode, HtmlNode> transform)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            return transform(document.DocumentNode).OuterHtml;
        }

        public static HtmlNode AddLinkType(this HtmlNode html, string locale)
        {
            AddLinkType(html, "a", "href", locale);
            AddLinkType(html, "img", "src", locale);
            return html;
        }

        public static long CountWord(this HtmlNode node)
        {
            // TODO: word count does not work for CJK locales...
            if (node.NodeType == HtmlNodeType.Comment)
                return 0;

            if (node is HtmlTextNode textNode)
                return CountWordInText(textNode.Text);

            var total = 0L;
            foreach (var child in node.ChildNodes)
            {
                total += CountWord(child);
            }
            return total;
        }

        public static HtmlNode RemoveRerunCodepenIframes(this HtmlNode html)
        {
            // the rerun button on codepen iframes isn't accessibile.
            // rather than get acc bugs or ban codepen, we're just hiding the rerun button using their iframe api
            foreach (var node in html.Descendants("iframe"))
            {
                var src = node.GetAttributeValue("src", null);
                if (src != null && src.Contains("codepen.io", StringComparison.OrdinalIgnoreCase))
                {
                    node.SetAttributeValue("src", src + "&rerun-position=hidden&");
                }
            }
            return html;
        }

        public static HtmlNode StripTags(this HtmlNode html)
        {
            var nodesToRemove = new List<HtmlNode>();

            foreach (var node in html.DescendantsAndSelf())
            {
                if (node.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
                    node.Name.Equals("link", StringComparison.OrdinalIgnoreCase) ||
                    node.Name.Equals("style", StringComparison.OrdinalIgnoreCase))
                {
                    nodesToRemove.Add(node);
                }
                else
                {
                    node.Attributes.Remove("style");
                }
            }

            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }
            return html;
        }

        public static HashSet<string> GetBookmarks(this HtmlNode html)
        {
            var result = new HashSet<string>();

            foreach (var node in html.DescendantsAndSelf())
            {
                var id = node.GetAttributeValue("id", "");
                if (!string.IsNullOrEmpty(id))
                {
                    result.Add(id);
                }
                var name = node.GetAttributeValue("name", "");
                if (!string.IsNullOrEmpty(name))
                {
                    result.Add(name);
                }
            }

            return result;
        }

        public static string TransformLinks(string html, Func<string, string> transform)
        {
            // Fast pass it does not have <a> tag or <img> tag
            if (!((html.Contains("<a", StringComparison.OrdinalIgnoreCase) && html.Contains("href", StringComparison.OrdinalIgnoreCase)) ||
                  (html.Contains("<img", StringComparison.OrdinalIgnoreCase) && html.Contains("src", StringComparison.OrdinalIgnoreCase))))
            {
                return html;
            }

            // <a>b</a> generates 3 inline markdown tokens: <a>, b, </a>.
            // `HtmlNode.OuterHtml` turns <a> into <a></a>, and generates <a></a>b</a> for the above input.
            // The following code ensures we preserve the original html when changing links.
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var pos = 0;
            var result = new StringBuilder(html.Length + 64);

            foreach (var node in doc.DocumentNode.Descendants())
            {
                var link = node.Name == "a" ? node.Attributes["href"]
                         : node.Name == "img" ? node.Attributes["src"]
                         : null;

                if (link is null)
                {
                    continue;
                }

                var valueStartIndex = s_getValueStartIndex(link);
                if (valueStartIndex > pos)
                {
                    result.Append(html, pos, valueStartIndex - pos);
                }
                var transformed = transform(HttpUtility.HtmlDecode(link.Value));
                if (!string.IsNullOrEmpty(transformed))
                {
                    result.Append(HttpUtility.HtmlEncode(transformed));
                }
                pos = valueStartIndex + link.Value.Length;
            }

            if (html.Length > pos)
            {
                result.Append(html, pos, html.Length - pos);
            }
            return result.ToString();
        }

        /// <summary>
        /// Get title and raw title, remove title node if all previous nodes are invisible
        /// </summary>
        public static bool TryExtractTitle(HtmlNode node, out string title, out string rawTitle)
        {
            var existVisibleNode = false;

            title = null;
            rawTitle = string.Empty;
            foreach (var child in node.ChildNodes)
            {
                if (!IsInvisibleNode(child))
                {
                    if (child.NodeType == HtmlNodeType.Element && (child.Name == "h1" || child.Name == "h2" || child.Name == "h3"))
                    {
                        title = child.InnerText == null ? null : HttpUtility.HtmlDecode(child.InnerText);

                        if (!existVisibleNode)
                        {
                            rawTitle = child.OuterHtml;
                            child.Remove();
                        }

                        return true;
                    }

                    existVisibleNode = true;
                }
            }

            return false;

            bool IsInvisibleNode(HtmlNode n)
            {
                return n.NodeType == HtmlNodeType.Comment ||
                    (n.NodeType == HtmlNodeType.Text && string.IsNullOrWhiteSpace(n.OuterHtml));
            }
        }

        private static void AddLinkType(this HtmlNode html, string tag, string attribute, string locale)
        {
            foreach (var node in html.Descendants(tag))
            {
                var href = node.GetAttributeValue(attribute, null);

                if (string.IsNullOrEmpty(href))
                {
                    continue;
                }

                switch (HrefUtility.GetHrefType(href))
                {
                    case HrefType.SelfBookmark:
                        node.SetAttributeValue("data-linktype", "self-bookmark");
                        break;
                    case HrefType.AbsolutePath:
                    case HrefType.WindowsAbsolutePath:
                        node.SetAttributeValue("data-linktype", "absolute-path");
                        node.SetAttributeValue(attribute, AddLocaleIfMissing(href, locale));
                        break;
                    case HrefType.RelativePath:
                        node.SetAttributeValue("data-linktype", "relative-path");
                        break;
                    case HrefType.External:
                        node.SetAttributeValue("data-linktype", "external");
                        break;
                }
            }
        }

        private static string AddLocaleIfMissing(string href, string locale)
        {
            try
            {
                var pos = href.IndexOfAny(new[] { '/', '\\' }, 1);
                if (pos >= 1)
                {
                    var urlLocale = href.Substring(1, pos - 1);
                    if (urlLocale.Contains("-"))
                    {
                        CultureInfo.GetCultureInfo(urlLocale);
                        return href;
                    }
                }
                return '/' + locale + href;
            }
            catch (CultureNotFoundException)
            {
                return '/' + locale + href;
            }
        }

        private static int CountWordInText(string text)
        {
            var total = 0;
            var word = false;

            foreach (var ch in text)
            {
                if (ch == ' ' || ch == '\t' || ch == '\n')
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

            if (word)
            {
                total++;
            }

            return total;
        }
    }
}
