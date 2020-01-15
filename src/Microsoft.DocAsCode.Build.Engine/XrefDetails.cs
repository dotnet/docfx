// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Web;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Common;

    using HtmlAgilityPack;

    [Serializable]
    public sealed class XRefDetails
    {
        public string Uid { get; private set; }
        public string Query { get; private set; } = string.Empty;
        public string Anchor { get; private set; } = string.Empty;
        public string Title { get; private set; }
        public string Href { get; private set; }
        public string Raw { get; private set; }
        public string RawSource { get; private set; }
        public string DisplayProperty { get; private set; } = "name";
        public string AltProperty { get; private set; } = "fullName";
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
            if (node.Name != "xref")
            {
                throw new NotSupportedException("Only xref node is supported!");
            }
            var rawUid = node.GetAttributeValue("uid", null);
            var xref = new XRefDetails
            {
                InnerHtml = node.InnerHtml,
                Uid = rawUid,
                SourceFile = node.GetAttributeValue("sourceFile", null),
                SourceStartLineNumber = node.GetAttributeValue("sourceStartLineNumber", 0),
                SourceEndLineNumber = node.GetAttributeValue("sourceEndLineNumber", 0),
                RawSource = node.GetAttributeValue("data-raw-source", null),
                ThrowIfNotResolved = node.GetAttributeValue("data-throw-if-not-resolved", false),
                TemplatePath = StringHelper.HtmlDecode(node.GetAttributeValue("template", null)),
            };

            var rawHref = node.GetAttributeValue("href", null);
            if (!string.IsNullOrEmpty(rawHref))
            {
                if (!string.IsNullOrEmpty(rawUid))
                {
                    Logger.LogWarning($"Both href and uid attribute are defined for {node.OuterHtml}, use href instead of uid.");
                }

                var (path, query, fragment) = UriUtility.Split(rawHref);
                xref.Uid = HttpUtility.UrlDecode(path);
                xref.Anchor = fragment;

                // extract values from query
                var queryValueCollection = HttpUtility.ParseQueryString(query);
                xref.DisplayProperty = ExtractValue(queryValueCollection, "displayProperty") ?? xref.DisplayProperty;
                xref.AltProperty = ExtractValue(queryValueCollection, "altProperty") ?? xref.AltProperty;
                xref.Text = StringHelper.HtmlEncode(ExtractValue(queryValueCollection, "text")) ?? xref.Text;
                xref.Alt = StringHelper.HtmlEncode(ExtractValue(queryValueCollection, "alt")) ?? xref.Alt;
                xref.Title = ExtractValue(queryValueCollection, "title") ?? xref.Title;

                var remainingQuery = queryValueCollection.ToString();
                xref.Query = string.IsNullOrEmpty(remainingQuery) ? string.Empty : "?" + remainingQuery;
            }

            // extract values from HTML attributes
            xref.DisplayProperty = node.GetAttributeValue("displayProperty", xref.DisplayProperty);
            xref.AltProperty = node.GetAttributeValue("altProperty", xref.AltProperty);
            xref.Text = node.GetAttributeValue("text", node.GetAttributeValue("name", xref.Text));
            xref.Alt = node.GetAttributeValue("alt", node.GetAttributeValue("fullname", xref.Alt));
            xref.Title = node.GetAttributeValue("title", xref.Title);

            // Both `data-raw-html` and `data-raw-source` are html encoded. Use `data-raw-html` with higher priority.
            // `data-raw-html` will be decoded then displayed, while `data-raw-source` will be displayed directly.
            var raw = node.GetAttributeValue("data-raw-html", null);
            if (!string.IsNullOrEmpty(raw))
            {
                xref.Raw = StringHelper.HtmlDecode(raw);
            }
            else
            {
                xref.Raw = xref.RawSource;
            }

            return xref;

            string ExtractValue(NameValueCollection collection, string properName)
            {
                var value = collection[properName];
                collection.Remove(properName);
                return value;
            }
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
                var href = UriUtility.MergeHref(spec.Href, Query + Anchor);
                (Href, Query, Anchor) = UriUtility.Split(href);
            }
            Spec = spec;
        }

        public (HtmlNode, bool resolved) ConvertToHtmlNode(string language, ITemplateRenderer renderer)
        {
            if (!string.IsNullOrEmpty(TemplatePath) && renderer != null && Spec != null)
            {
                if (Spec != null)
                {
                    var converted = renderer.Render(Spec);
                    var node = new HtmlDocument();
                    node.LoadHtml(converted);
                    return (node.DocumentNode, true);
                }
                else
                {
                    Logger.LogWarning($"Invalid xref definition \"{Raw}\", XrefSpec is not defined.");
                }
            }

            if (!string.IsNullOrEmpty(Href))
            {
                if (!string.IsNullOrEmpty(InnerHtml))
                {
                    return (GetAnchorNode(Href, Query + Anchor, Title, InnerHtml, RawSource, SourceFile, SourceStartLineNumber, SourceEndLineNumber), true);
                }
                if (!string.IsNullOrEmpty(Text))
                {
                    return (GetAnchorNode(Href, Query + Anchor, Title, Text, RawSource, SourceFile, SourceStartLineNumber, SourceEndLineNumber), true);
                }
                if (Spec != null)
                {
                    var value = StringHelper.HtmlEncode(GetLanguageSpecificAttribute(Spec, language, DisplayProperty, "name"));
                    if (!string.IsNullOrEmpty(value))
                    {
                        return (GetAnchorNode(Href, Query + Anchor, Title, value, RawSource, SourceFile, SourceStartLineNumber, SourceEndLineNumber), true);
                    }
                }
                return (GetAnchorNode(Href, Query + Anchor, Title, Uid, RawSource, SourceFile, SourceStartLineNumber, SourceEndLineNumber), true);
            }
            else
            {
                if (!string.IsNullOrEmpty(Raw))
                {
                    return (HtmlNode.CreateNode(Raw), false);
                }
                if (!string.IsNullOrEmpty(InnerHtml))
                {
                    return (GetDefaultPlainTextNode(InnerHtml), false);
                }
                if (!string.IsNullOrEmpty(Alt))
                {
                    return (GetDefaultPlainTextNode(Alt), false);
                }
                if (Spec != null)
                {
                    var value = StringHelper.HtmlEncode(GetLanguageSpecificAttribute(Spec, language, AltProperty, "name"));
                    if (!string.IsNullOrEmpty(value))
                    {
                        return (GetDefaultPlainTextNode(value), false);
                    }
                }
                return (GetDefaultPlainTextNode(Uid), false);
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
                if (spec.TryGetXrefStringValue(keyWithSuffix, out var suffixedValue))
                {
                    return suffixedValue;
                }
                if (spec.TryGetXrefStringValue(key, out var value))
                {
                    return value;
                }
            }
            return null;
        }

        public static HtmlNode ConvertXrefLinkNodeToXrefNode(HtmlNode node)
        {
            var href = node.GetAttributeValue("href", null);
            if (node.Name != "a" || string.IsNullOrEmpty(href) || !href.StartsWith("xref:", StringComparison.OrdinalIgnoreCase))
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
