// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using HtmlAgilityPack;

namespace Microsoft.Docs.Build
{
    internal static class HtmlUtility
    {
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

        public static string GetInnerText(this HtmlNode html)
        {
            return html.InnerText;
        }

        public static HtmlNode AddLinkType(this HtmlNode html, string locale, bool shouldToLowerCase = true)
        {
            AddLinkType(html, "a", "href", locale, shouldToLowerCase);
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

        public static IEnumerable<string> GetBookmarks(this HtmlNode html)
        {
            foreach (var node in html.DescendantsAndSelf())
            {
                var id = node.GetAttributeValue("id", "");
                if (!string.IsNullOrEmpty(id))
                {
                    yield return id;
                }
                var name = node.GetAttributeValue("name", "");
                if (!string.IsNullOrEmpty(name))
                {
                    yield return name;
                }
            }
        }

        public static string TransformLinks(this string html, Func<string, string> transform)
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

                if (link == null)
                {
                    continue;
                }
                if (link.ValueStartIndex > pos)
                {
                    result.Append(html, pos, link.ValueStartIndex - pos);
                }
                result.Append(transform(link.Value));
                pos = link.ValueStartIndex + link.ValueLength;
            }

            if (html.Length > pos)
            {
                result.Append(html, pos, html.Length - pos);
            }
            return result.ToString();
        }

        private static void AddLinkType(this HtmlNode html, string tag, string attribute, string locale, bool shouldToLowerCase = true)
        {
            foreach (var node in html.Descendants(tag))
            {
                var href = node.GetAttributeValue(attribute, null);
                if (string.IsNullOrEmpty(href))
                {
                    continue;
                }
                if (href[0] == '#')
                {
                    node.SetAttributeValue("data-linktype", "self-bookmark");
                    continue;
                }
                if (href[0] == '/' || href[0] == '\\')
                {
                    node.SetAttributeValue(attribute, AddLocaleIfMissing(shouldToLowerCase ? HrefToLower(href) : href, locale));
                    node.SetAttributeValue("data-linktype", "absolute-path");
                    continue;
                }
                if (Uri.TryCreate(href, UriKind.Absolute, out _))
                {
                    node.SetAttributeValue("data-linktype", "external");
                    continue;
                }
                node.SetAttributeValue(attribute, shouldToLowerCase ? HrefToLower(href) : href);
                node.SetAttributeValue("data-linktype", "relative-path");
            }
        }

        private static string HrefToLower(string href)
        {
            // TODO: legacy only, should not touch href
            var i = href.IndexOfAny(new[] { '#', '?' });
            return i >= 0 ? href.Substring(0, i).ToLowerInvariant() + href.Substring(i) : href.ToLowerInvariant();
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
