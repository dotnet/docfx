// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Text.RegularExpressions;
    using System.Web;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    internal sealed class XRefDetails
    {
        /// <summary>
        /// TODO: completely move into template
        /// Must be consistent with template input.replace(/\W/g, '_');
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static Regex HtmlEncodeRegex = new Regex(@"\W", RegexOptions.Compiled);

        public string Uid { get; private set; }
        public string Anchor { get; private set; }
        public string PlainTextDisplayName { get; private set; }
        public string AnchorDisplayName { get; private set; }
        public string Title { get; private set; }
        public string Href { get; private set; }
        public string Raw { get; private set; }
        public XRefSpec Spec { get; private set; }
        public bool ThrowIfNotResolved { get; private set; }

        private XRefDetails() { }

        public static XRefDetails From(HtmlAgilityPack.HtmlNode node)
        {
            if (node.Name != "xref") throw new NotSupportedException("Only xref node is supported!");
            var xref = new XRefDetails();

            var rawUid = node.GetAttributeValue("href", null);
            if (!string.IsNullOrEmpty(rawUid))
            {
                var anchorIndex = rawUid.IndexOf("#");
                if (anchorIndex == -1)
                {
                    xref.Anchor = string.Empty;
                    xref.Uid = HttpUtility.UrlDecode(rawUid);
                }
                else
                {
                    xref.Anchor = rawUid.Substring(anchorIndex);
                    xref.Uid = HttpUtility.UrlDecode(rawUid.Remove(anchorIndex));
                }
            }

            var overrideName = node.InnerText;
            if (!string.IsNullOrEmpty(overrideName))
            {
                xref.AnchorDisplayName = overrideName;
                xref.PlainTextDisplayName = overrideName;
            }
            else
            {
                // If name | fullName exists, use the one from xref because spec name is different from name for generic types
                // e.g. return type: IEnumerable<T>, spec name should be IEnumerable
                xref.AnchorDisplayName = node.GetAttributeValue("name", null);
                xref.PlainTextDisplayName = node.GetAttributeValue("fullName", null);
            }

            xref.Title = node.GetAttributeValue("title", null);

            // Both `data-raw-html` and `data-raw` are html encoded. Use `data-raw-html` with higher priority.
            // `data-raw-html` will be decoded then displayed, while `data-raw` will be displayed directly.
            var raw = node.GetAttributeValue("data-raw-html", null);
            if (!string.IsNullOrEmpty(raw))
            {
                xref.Raw = StringHelper.HtmlDecode(raw);
            }
            else
            {
                xref.Raw = node.GetAttributeValue("data-raw", null);
            }

            xref.ThrowIfNotResolved = node.GetAttributeValue("data-throw-if-not-resolved", false);

            return xref;
        }

        public void ApplyXrefSpec(XRefSpec spec)
        {
            if (spec == null) return;
            var href = spec.Href;
            if (PathUtility.IsRelativePath(href))
            {
                if (string.IsNullOrEmpty(Anchor))
                {
                    // TODO: hashtag from tempalte
                    // TODO: What if href is not html?
                    Anchor = "#" + GetHtmlId(Uid);
                }
            }
            Href = HttpUtility.UrlDecode(href);
            Spec = spec;
        }

        /// <summary>
        /// TODO: multi-lang support
        /// </summary>
        /// <returns></returns>
        public HtmlAgilityPack.HtmlNode ConvertToHtmlNode(string language)
        {
            // If href exists, return anchor else return text
            if (!string.IsNullOrEmpty(Href))
            {
                string value = AnchorDisplayName;
                if (string.IsNullOrEmpty(value))
                {
                    value = PlainTextDisplayName ?? Uid;
                    if (Spec != null)
                    {
                        value = StringHelper.HtmlEncode(GetLanguageSpecificAttribute(Spec, language, value, "name"));
                    }
                }
                return GetAnchorNode(Href, Anchor, Title, value);
            }
            else
            {
                if (!string.IsNullOrEmpty(Raw))
                {
                    return HtmlAgilityPack.HtmlNode.CreateNode(Raw);
                }

                string value = PlainTextDisplayName;
                if (string.IsNullOrEmpty(value))
                {
                    value = AnchorDisplayName ?? Uid;
                    if (Spec != null)
                    {
                        value = StringHelper.HtmlEncode(GetLanguageSpecificAttribute(Spec, language, value, "fullName", "name"));
                    }
                }

                return GetDefaultPlainTextNode(value);
            }
        }

        public static string GetHtmlId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return HtmlEncodeRegex.Replace(id, "_");
        }

        private static HtmlAgilityPack.HtmlNode GetAnchorNode(string href, string anchor, string title, string value)
        {
            var anchorNode = $"<a class=\"xref\" href=\"{href}\"";
            if (!string.IsNullOrEmpty(anchor))
            {
                anchorNode += $" anchor=\"{anchor}\"";
            }
            if (!string.IsNullOrEmpty(title))
            {
                anchorNode += $" title=\"{title}\"";
            }

            anchorNode += $">{value}</a>";

            return HtmlAgilityPack.HtmlNode.CreateNode(anchorNode);
        }

        private static HtmlAgilityPack.HtmlNode GetDefaultPlainTextNode(string value)
        {
            var spanNode = $"<span class=\"xref\">{value}</span>";
            return HtmlAgilityPack.HtmlNode.CreateNode(spanNode);
        }

        private static string GetLanguageSpecificAttribute(XRefSpec spec, string language, string defaultValue, params string[] keyInFallbackOrder)
        {
            if (keyInFallbackOrder == null || keyInFallbackOrder.Length == 0) throw new ArgumentException("key must be provided!", nameof(keyInFallbackOrder));
            string suffix = string.Empty;
            if (!string.IsNullOrEmpty(language)) suffix = "." + language;
            foreach (var key in keyInFallbackOrder)
            {
                string value;
                var keyWithSuffix = key + suffix;
                if (spec.TryGetValue(keyWithSuffix, out value))
                {
                    return value;
                }
                if (spec.TryGetValue(key, out value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        public static HtmlAgilityPack.HtmlNode ConvertXrefLinkNodeToXrefNode(HtmlAgilityPack.HtmlNode node)
        {
            var href = node.GetAttributeValue("href", null);
            if (node.Name != "a" || string.IsNullOrEmpty(href) || !href.StartsWith("xref:"))
            {
                throw new NotSupportedException("Only anchor node with href started with \"xref:\" is supported!");
            }
            href = href.Substring("xref:".Length);
            var raw = StringHelper.HtmlEncode(node.OuterHtml);
            var title = node.GetAttributeValue("title", null);

            var xrefNode = $"<xref href=\"{href}\" data-throw-if-not-resolved=\"True\" data-raw-html=\"{raw}\"";
            if (!string.IsNullOrEmpty(title))
            {
                xrefNode += $" title=\"{title}\"";
            }
            xrefNode += $">{node.InnerText}</xref>";

            return HtmlAgilityPack.HtmlNode.CreateNode(xrefNode);
        }
    }
}
