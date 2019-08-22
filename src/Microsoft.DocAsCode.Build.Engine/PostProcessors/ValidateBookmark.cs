// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public sealed class ValidateBookmark : HtmlDocumentHandler
    {
        private static readonly string XPathTemplate = "//*/@{0}";
        private static readonly HashSet<string> WhiteList = new HashSet<string> { "top" };
        /// <summary>
        /// bookmarks mapping from output file -> bookmarks
        /// </summary>
        private readonly OSPlatformSensitiveDictionary<HashSet<string>> _registeredBookmarks =
            new OSPlatformSensitiveDictionary<HashSet<string>>();
        /// <summary>
        /// file mapping from output file -> src file
        /// </summary>
        private readonly OSPlatformSensitiveDictionary<string> _fileMapping =
            new OSPlatformSensitiveDictionary<string>();
        private OSPlatformSensitiveDictionary<List<LinkItem>> _linksWithBookmark =
            new OSPlatformSensitiveDictionary<List<LinkItem>>();

        #region IHtmlDocumentHandler members

        public override void LoadContext(HtmlPostProcessContext context)
        {
            if (context.PostProcessorHost?.IsIncremental != true)
            {
                return;
            }
            var fileMapping = Deserialize<string>(context, nameof(_fileMapping)) ?? new OSPlatformSensitiveDictionary<string>();
            var registeredBookmarks = Deserialize<HashSet<string>>(context, nameof(_registeredBookmarks)) ?? new OSPlatformSensitiveDictionary<HashSet<string>>();
            var set = new HashSet<string>(
                from sfi in context.PostProcessorHost.SourceFileInfos
                where sfi.IsIncremental
                select sfi.SourceRelativePath,
                FilePathComparer.OSPlatformSensitiveStringComparer);
            foreach (var pair in fileMapping)
            {
                if (set.Contains(pair.Value))
                {
                    _fileMapping[pair.Key] = pair.Value;
                }
            }
            foreach (var pair in registeredBookmarks)
            {
                if (set.Contains(fileMapping[pair.Key]))
                {
                    _registeredBookmarks[pair.Key] = pair.Value;
                }
            }
        }

        protected override void HandleCore(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile)
        {
            _fileMapping[outputFile] = inputFile;

            // RFC 3986: relative-ref = relative-part [ "?" query ] [ "#" fragment ]
            _linksWithBookmark[outputFile] =
                (from node in GetNodesWithAttribute(document, "href")
                 let nocheck = node.GetAttributeValue("nocheck", null)
                 where !"bookmark".Equals(nocheck, StringComparison.OrdinalIgnoreCase)
                 let link = node.GetAttributeValue("href", null)
                 let bookmark = UriUtility.GetFragment(link).TrimStart('#')
                 let decodedLink = RelativePath.TryParse(HttpUtility.UrlDecode(UriUtility.GetPath(link)))
                 where !string.IsNullOrEmpty(bookmark) && !WhiteList.Contains(bookmark)
                 where decodedLink != null
                 select new LinkItem
                 {
                     Title = node.InnerText,
                     Href = TransformPath(outputFile, decodedLink),
                     Bookmark = bookmark,
                     SourceFragment = WebUtility.HtmlDecode(node.GetAttributeValue("data-raw-source", null)),
                     SourceFile = WebUtility.HtmlDecode(node.GetAttributeValue("sourceFile", null)),
                     SourceLineNumber = node.GetAttributeValue("sourceStartLineNumber", 0),
                     TargetLineNumber = node.Line
                 }).ToList();
            var anchors = GetNodeAttribute(document, "id").Concat(GetNodeAttribute(document, "name"));
            if (manifestItem.Metadata.TryGetValue("rawTitle", out object rawTitleString))
            {
                var rawTitle = new HtmlDocument();
                rawTitle.LoadHtml(rawTitleString.ToString());
                anchors = anchors.Concat(GetNodeAttribute(rawTitle, "id"));
            }
            _registeredBookmarks[outputFile] = new HashSet<string>(anchors);
        }

        protected override Manifest PostHandleCore(Manifest manifest)
        {
            foreach (var pair in _linksWithBookmark)
            {
                string currentFile = pair.Key;
                foreach (var linkItem in pair.Value)
                {
                    string title = linkItem.Title;
                    string linkedToFile = linkItem.Href;
                    string bookmark = linkItem.Bookmark;
                    if (_registeredBookmarks.TryGetValue(linkedToFile, out HashSet<string> bookmarks) && !bookmarks.Contains(bookmark))
                    {
                        string currentFileSrc = linkItem.SourceFile ?? _fileMapping[currentFile];
                        string linkedToFileSrc = _fileMapping[linkedToFile];

                        bool internalBookmark = FilePathComparer.OSPlatformSensitiveStringComparer.Equals(linkedToFileSrc, _fileMapping[currentFile]);

                        string link = internalBookmark ? $"#{bookmark}" : $"{linkedToFileSrc}#{bookmark}";
                        string content = linkItem.SourceFragment;
                        if (string.IsNullOrEmpty(content))
                        {
                            // Invalid bookmarks introduced from templates is a corner case, ignored.
                            content = $"<a href=\"{link}\">{title}</a>";
                        }

                        Logger.LogWarning($"Invalid link: '{content}'. The file {linkedToFileSrc} doesn't contain a bookmark named '{bookmark}'.",
                            null,
                            currentFileSrc,
                            linkItem.SourceLineNumber != 0 ? linkItem.SourceLineNumber.ToString() : null,
                            code: WarningCodes.Build.InvalidBookmark);
                    }
                }
            }
            return manifest;
        }

        public override void SaveContext(HtmlPostProcessContext context)
        {
            context.Save(nameof(_registeredBookmarks), stream => Serialize(stream, _registeredBookmarks));
            context.Save(nameof(_fileMapping), stream => Serialize(stream, _fileMapping));
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

        private static string TransformPath(string basePathFromRoot, RelativePath relativePath)
        {
            // Special logic for `RelativePath.Empty`: "C/d.html" + "" -> "C" rather than "C/d.html"
            if (relativePath == RelativePath.Empty)
            {
                return ((RelativePath)basePathFromRoot).RemoveWorkingFolder();
            }
            return ((RelativePath)basePathFromRoot + relativePath).RemoveWorkingFolder();
        }

        private static OSPlatformSensitiveDictionary<T> Deserialize<T>(HtmlPostProcessContext context, string name)
            where T : class
        {
            return context.Load(
                name,
                stream =>
                {
                    using (var sr = new StreamReader(stream))
                    {
                        return JsonUtility.Deserialize<OSPlatformSensitiveDictionary<T>>(sr);
                    }
                });
        }

        private static void Serialize(Stream stream, object obj)
        {
            using (var sw = new StreamWriter(stream))
            {
                JsonUtility.Serialize(sw, obj);
            }
        }

        private class LinkItem
        {
            public string Title { get; set; }

            public string Href { get; set; }

            public string Bookmark { get; set; }

            public string SourceFragment { get; set; }

            public string SourceFile { get; set; }

            public int SourceLineNumber { get; set; }

            public int TargetLineNumber { get; set; }
        }
    }
}
