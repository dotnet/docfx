// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Web;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Common;

    using HtmlAgilityPack;

    [Serializable]
    public sealed class XRefDetails
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
        public string Title { get; private set; }
        public string Href { get; private set; }
        public string Raw { get; private set; }
        public string RawSource { get; private set; }
        public string DisplayProperty { get; private set; }
        public string AltProperty { get; private set; }
        public string InnerHtml { get; private set; }
        public string Text { get; private set; }
        public string Alt { get; private set; }
        public XRefSpec Spec { get; private set; }
        public bool ThrowIfNotResolved { get; private set; }
        public string SourceFile { get; private set; }
        public int SourceStartLineNumber { get; private set; }
        public int SourceEndLineNumber { get; private set; }
        public string TemplatePath { get; private set; }

        private XRefDetails() { }

        public static XRefDetails From(HtmlNode node)
        {
            if (node.Name != "xref") throw new NotSupportedException("Only xref node is supported!");
            var xref = new XRefDetails();
            var uid = node.GetAttributeValue("uid", null);
            var rawHref = node.GetAttributeValue("href", null);
            NameValueCollection queryString = null;

            if (!string.IsNullOrEmpty(rawHref))
            {
                if (!string.IsNullOrEmpty(uid))
                {
                    Logger.LogWarning($"Both href and uid attribute are defined for {node.OuterHtml}, use href instead of uid.");
                }

                string others;
                var anchorIndex = rawHref.IndexOf("#");
                if (anchorIndex == -1)
                {
                    xref.Anchor = string.Empty;
                    others = rawHref;
                }
                else
                {
                    xref.Anchor = rawHref.Substring(anchorIndex);
                    others = rawHref.Remove(anchorIndex);
                }
                var queryIndex = others.IndexOf("?");
                if (queryIndex == -1)
                {
                    xref.Uid = HttpUtility.UrlDecode(others);
                }
                else
                {
                    xref.Uid = HttpUtility.UrlDecode(others.Remove(queryIndex));
                    queryString = HttpUtility.ParseQueryString(others.Substring(queryIndex));
                }
            }
            else
            {
                xref.Uid = uid;
            }

            xref.InnerHtml = node.InnerHtml;
            xref.DisplayProperty = node.GetAttributeValue("displayProperty", queryString?.Get("displayProperty") ?? XRefSpec.NameKey);
            xref.AltProperty = node.GetAttributeValue("altProperty", queryString?.Get("altProperty") ?? "fullName");
            xref.Text = node.GetAttributeValue("text", node.GetAttributeValue("name", StringHelper.HtmlEncode(queryString?.Get("text"))));
            xref.Alt = node.GetAttributeValue("alt", node.GetAttributeValue("fullname", StringHelper.HtmlEncode(queryString?.Get("alt"))));

            xref.Title = node.GetAttributeValue("title", queryString?.Get("title"));
            xref.SourceFile = node.GetAttributeValue("sourceFile", null);
            xref.SourceStartLineNumber = node.GetAttributeValue("sourceStartLineNumber", 0);
            xref.SourceEndLineNumber = node.GetAttributeValue("sourceEndLineNumber", 0);

            // Both `data-raw-html` and `data-raw-source` are html encoded. Use `data-raw-html` with higher priority.
            // `data-raw-html` will be decoded then displayed, while `data-raw-source` will be displayed directly.
            xref.RawSource = node.GetAttributeValue("data-raw-source", null);
            var raw = node.GetAttributeValue("data-raw-html", null);
            if (!string.IsNullOrEmpty(raw))
            {
                xref.Raw = StringHelper.HtmlDecode(raw);
            }
            else
            {
                xref.Raw = xref.RawSource;
            }

            xref.ThrowIfNotResolved = node.GetAttributeValue("data-throw-if-not-resolved", false);
            var templatePath = node.GetAttributeValue("template", null);
            if (templatePath != null)
            {
                xref.TemplatePath = StringHelper.HtmlDecode(templatePath);
            }

            return xref;
        }

        public void ApplyXrefSpec(XRefSpec spec)
        {
            if (spec == null)
            {
                return;
            }

            // TODO: What if href is not html?
            if (!string.IsNullOrEmpty(spec.Href))
            {
                Href = UriUtility.GetNonFragment(spec.Href);
                if (string.IsNullOrEmpty(Anchor))
                {
                    Anchor = UriUtility.GetFragment(spec.Href);
                }
            }
            Spec = spec;
        }

        /// <summary>
        /// TODO: multi-lang support
        /// </summary>
        /// <returns></returns>
        public HtmlNode ConvertToHtmlNode(string language, ITemplateRenderer renderer)
        {
            if (!string.IsNullOrEmpty(TemplatePath) && renderer != null && Spec != null)
            {
                if (Spec != null)
                {
                    var converted = renderer.Render(Spec);
                    if (string.IsNullOrWhiteSpace(converted))
                    {
                        Logger.LogWarning($"{Spec.Uid} is rendered to empty with template {TemplatePath} for {Raw ?? RawSource}.");
                    }
                    var node = new HtmlDocument();
                    node.LoadHtml(converted);
                    return node.DocumentNode;
                }
                else
                {
                    Logger.LogWarning($"Invalid xref definition \"{Raw}\", XrefSpec is not defined.");
                }
            }

            // If href exists, return anchor else return text
            if (!string.IsNullOrEmpty(Href))
            {
                if (!string.IsNullOrEmpty(InnerHtml))
                {
                    return GetAnchorNode(Href, Anchor, Title, InnerHtml, RawSource, SourceFile, SourceStartLineNumber, SourceEndLineNumber);
                }
                if (!string.IsNullOrEmpty(Text))
                {
                    return GetAnchorNode(Href, Anchor, Title, Text, RawSource, SourceFile, SourceStartLineNumber, SourceEndLineNumber);
                }
                if (Spec != null)
                {
                    var value = StringHelper.HtmlEncode(GetLanguageSpecificAttribute(Spec, language, DisplayProperty, "name"));
                    if (!string.IsNullOrEmpty(value))
                    {
                        return GetAnchorNode(Href, Anchor, Title, value, RawSource, SourceFile, SourceStartLineNumber, SourceEndLineNumber);
                    }
                }
                return GetAnchorNode(Href, Anchor, Title, Uid, RawSource, SourceFile, SourceStartLineNumber, SourceEndLineNumber);
            }
            else
            {
                if (!string.IsNullOrEmpty(Raw))
                {
                    return HtmlNode.CreateNode(Raw);
                }
                if (!string.IsNullOrEmpty(InnerHtml))
                {
                    return GetDefaultPlainTextNode(InnerHtml);
                }
                if (!string.IsNullOrEmpty(Alt))
                {
                    return GetDefaultPlainTextNode(Alt);
                }
                if (Spec != null)
                {
                    var value = StringHelper.HtmlEncode(GetLanguageSpecificAttribute(Spec, language, AltProperty, "name"));
                    if (!string.IsNullOrEmpty(value))
                    {
                        return GetDefaultPlainTextNode(value);
                    }
                }
                return GetDefaultPlainTextNode(Uid);
            }
        }

        private static HtmlNode GetAnchorNode(string href, string anchor, string title, string value, string rawSource, string sourceFile, int sourceStartLineNumber, int sourceEndLineNumber)
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
            if (!string.IsNullOrEmpty(rawSource))
            {
                anchorNode += $" data-raw-source=\"{rawSource}\"";
            }
            if (!string.IsNullOrEmpty(sourceFile))
            {
                anchorNode += $" sourceFile=\"{sourceFile}\"";
            }
            if (sourceStartLineNumber != 0)
            {
                anchorNode += $" sourceStartLineNumber={sourceStartLineNumber}";
            }
            if (sourceEndLineNumber != 0)
            {
                anchorNode += $" sourceEndLineNumber={sourceEndLineNumber}";
            }

            anchorNode += $">{value}</a>";

            return HtmlNode.CreateNode(anchorNode);
        }

        private static HtmlNode GetDefaultPlainTextNode(string value)
        {
            var spanNode = $"<span class=\"xref\">{value}</span>";
            return HtmlNode.CreateNode(spanNode);
        }

        private static string GetLanguageSpecificAttribute(XRefSpec spec, string language, params string[] keyInFallbackOrder)
        {
            if (keyInFallbackOrder == null || keyInFallbackOrder.Length == 0)
            {
                throw new ArgumentException("key must be provided!", nameof(keyInFallbackOrder));
            }
            string suffix = string.Empty;
            if (!string.IsNullOrEmpty(language))
            {
                suffix = "." + language;
            }
            foreach (var key in keyInFallbackOrder)
            {
                var keyWithSuffix = key + suffix;
                if (spec.TryGetValue(keyWithSuffix, out string value))
                {
                    return value;
                }
                if (spec.TryGetValue(key, out value))
                {
                    return value;
                }
            }
            return null;
        }

        public static HtmlNode ConvertXrefLinkNodeToXrefNode(HtmlNode node)
        {
            var href = node.GetAttributeValue("href", null);
            if (node.Name != "a" || string.IsNullOrEmpty(href) || !href.StartsWith("xref:"))
            {
                throw new NotSupportedException("Only anchor node with href started with \"xref:\" is supported!");
            }
            href = href.Substring("xref:".Length);
            var raw = StringHelper.HtmlEncode(node.OuterHtml);

            var xrefNode = $"<xref href=\"{href}\" data-throw-if-not-resolved=\"True\" data-raw-html=\"{raw}\"";
            foreach (var attr in node.Attributes ?? Enumerable.Empty<HtmlAttribute>())
            {
                if (attr.Name == "href" || attr.Name == "data-throw-if-not-resolved" || attr.Name == "data-raw-html")
                {
                    continue;
                }
                xrefNode += $" {attr.Name}=\"{attr.Value}\"";
            }
            xrefNode += $">{node.InnerHtml}</xref>";

            return HtmlNode.CreateNode(xrefNode);
        }
    }
}
