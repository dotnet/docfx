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
        public static string TransformHtml(string html, Func<HtmlNode, HtmlNode> transform)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return transform(doc.DocumentNode).OuterHtml;
        }

        public static HtmlNode AddLinkType(this HtmlNode html, string locale)
        {
            AddLinkType(html, "a", "href", locale);
            AddLinkType(html, "img", "src", locale);
            return html;
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
                if (href[0] == '#')
                {
                    node.SetAttributeValue("data-linktype", "self-bookmark");
                    continue;
                }
                if (href.Contains(":"))
                {
                    node.SetAttributeValue("data-linktype", "external");
                    continue;
                }
                if (href[0] == '/' || href[0] == '\\')
                {
                    node.SetAttributeValue(attribute, AddLocaleIfMissing(HrefToLower(href), locale));
                    node.SetAttributeValue("data-linktype", "absolute-path");
                    continue;
                }
                node.SetAttributeValue(attribute, HrefToLower(href));
                node.SetAttributeValue("data-linktype", "relative-path");
            }
        }

        private static string HrefToLower(string href)
        {
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

        public static HtmlNode RemoveRerunCodepenIframes(this HtmlNode html)
        {
            // the rerun button on codepen iframes isn't accessibile.
            // rather than get acc bugs or ban codepen, we're just hiding the rerun button using their iframe api
            foreach (var node in html.Descendants("iframe"))
            {
                var src = node.GetAttributeValue("src", null);
                if (src != null && src.Contains("codepen.io"))
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

        public static string TransformLinks(this string html, Func<string, string> transform)
        {
            // Fast pass it does not have <a> tag or <img> tag
            if (!((html.Contains("<a") && html.Contains("href")) ||
                  (html.Contains("<img") && html.Contains("src"))))
            {
                return html;
            }

            // <a>b</a> generates 3 inline markdown tokens: <a>, b, </a>.
            // `HtmlNode.OuterHtml` turns <a> into <a></a>, and generates <a></a>b</a> for the above input.
            // The following code ensures we preserve the original html when changing links.
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var links = new List<HtmlAttribute>();
            foreach (var node in doc.DocumentNode.Descendants("a"))
            {
                var href = node.Attributes["href"];
                if (href != null)
                {
                    links.Add(href);
                }
            }

            foreach (var node in doc.DocumentNode.Descendants("img"))
            {
                var href = node.Attributes["src"];
                if (href != null)
                {
                    links.Add(href);
                }
            }

            var pos = 0;
            var result = new StringBuilder(html.Length + links.Count * 16);

            foreach (var link in links)
            {
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
    }
}
