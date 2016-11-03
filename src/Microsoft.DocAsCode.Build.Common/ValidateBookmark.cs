﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    public class ValidateBookmark : HtmlDocumentHandler
    {
        private static readonly string XPathTemplate = "//*/@{0}";
        private static readonly HashSet<string> WhiteList = new HashSet<string> { "top" };
        private Dictionary<string, HashSet<string>> _registeredBookmarks;
        private Dictionary<string, List<LinkItem>> _linksWithBookmark;
        private Dictionary<string, string> _fileMapping;

        #region IHtmlDocumentHandler members

        public override Manifest PreHandle(Manifest manifest)
        {
            _registeredBookmarks = new Dictionary<string, HashSet<string>>(FilePathComparer.OSPlatformSensitiveStringComparer);
            _linksWithBookmark = new Dictionary<string, List<LinkItem>>(FilePathComparer.OSPlatformSensitiveStringComparer);
            _fileMapping = new Dictionary<string, string>(FilePathComparer.OSPlatformSensitiveStringComparer);
            return manifest;
        }

        public override void Handle(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile)
        {
            _fileMapping[outputFile] = inputFile;

            // RFC 3986: relative-ref = relative-part [ "?" query ] [ "#" fragment ]
            _linksWithBookmark[outputFile] =
                (from node in GetNodesWithAttribute(document, "href")
                 let link = node.GetAttributeValue("href", null)
                 let title = node.InnerText
                 let bookmarkIndex = link.IndexOf("#")
                 where bookmarkIndex != -1 && PathUtility.IsRelativePath(link)
                 let index = link.IndexOfAny(new[] { '?', '#' })
                 select new LinkItem { Title = title, Href = TransformPath(outputFile, HttpUtility.UrlDecode(link.Remove(index))), Bookmark = link.Substring(bookmarkIndex + 1), SourceFile = node.GetAttributeValue("sourceFile", null), SourceLineNumber = node.GetAttributeValue("sourceStartLineNumber", 0), TargetLineNumber = node.Line } into item
                 where !WhiteList.Contains(item.Bookmark)
                 select item).ToList();
            var anchors = GetNodeAttribute(document, "id").Concat(GetNodeAttribute(document, "name"));
            _registeredBookmarks[outputFile] = new HashSet<string>(anchors);
        }

        public override Manifest PostHandle(Manifest manifest)
        {
            foreach (var pair in _linksWithBookmark)
            {
                string currentFile = pair.Key;
                foreach (var linkItem in pair.Value)
                {
                    string title = linkItem.Title;
                    string linkedToFile = linkItem.Href == string.Empty ? currentFile : linkItem.Href;
                    string bookmark = linkItem.Bookmark;
                    HashSet<string> bookmarks;
                    if (_registeredBookmarks.TryGetValue(linkedToFile, out bookmarks) && !bookmarks.Contains(bookmark))
                    {
                        string currentFileSrc = linkItem.SourceFile ?? _fileMapping[currentFile];
                        string linkedToFileSrc = _fileMapping[linkedToFile];
                        if (linkItem.SourceLineNumber == 0)
                        {
                            Logger.LogWarning($"{currentFile} contains illegal link: {linkItem.Href}#{bookmark}. Link text is `{title}`. The file {linkedToFile} doesn't contain a bookmark named {bookmark}, please check the src file {currentFileSrc} and src linkedTo file {linkedToFileSrc} or the template you applied.", file: currentFile, line: linkItem.TargetLineNumber.ToString());
                        }
                        else
                        {
                            string link = linkItem.Href == string.Empty ? $"#{bookmark}" : $"{linkedToFileSrc}#{bookmark}";
                            Logger.LogWarning($"{currentFileSrc} contains illegal link: {link}. Link text is `{title}`. The file {linkedToFileSrc} doesn't contain a bookmark named {bookmark}.", file: currentFileSrc, line: linkItem.SourceLineNumber.ToString());
                        }
                    }
                }
            }
            return manifest;
        }

        #endregion

        private static IEnumerable<string> GetNodeAttribute(HtmlDocument html, string attribute)
        {
            var nodes = GetNodesWithAttribute(html, attribute);

            return nodes.Select(n => n.GetAttributeValue(attribute, null));
        }

        private static IEnumerable<HtmlNode> GetNodesWithAttribute(HtmlDocument html, string attribute)
        {
            return html.DocumentNode.SelectNodes(string.Format(XPathTemplate, attribute)) ?? Enumerable.Empty<HtmlNode>();
        }

        private static string TransformPath(string basePathFromRoot, string relativePath)
        {
            return ((RelativePath)basePathFromRoot + (RelativePath)relativePath).RemoveWorkingFolder();
        }

        private class LinkItem
        {
            public string Title { get; set; }

            public string Href { get; set; }

            public string Bookmark { get; set; }

            public string SourceFile { get; set; }

            public int SourceLineNumber { get; set; }

            public int TargetLineNumber { get; set; }
        }
    }
}
