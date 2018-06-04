// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using HtmlAgilityPack;

namespace Microsoft.Docs.Build
{
    internal static class HtmlUtility
    {
        public static void AddLinkType(HtmlNode html, string locale)
        {
            AddLinkType(html, "a", "href", locale);
            AddLinkType(html, "img", "src", locale);
        }

        private static void AddLinkType(HtmlNode html, string tag, string attribute, string locale)
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

        public static void StripTags(HtmlNode html)
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
        }
    }
}
