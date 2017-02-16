// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
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
        private OSPlatformSensitiveDictionary<HashSet<string>> _registeredBookmarks;
        private OSPlatformSensitiveDictionary<string> _fileMapping;
        private OSPlatformSensitiveDictionary<List<LinkItem>> _linksWithBookmark =
            new OSPlatformSensitiveDictionary<List<LinkItem>>();

        #region IHtmlDocumentHandler members

        public override void LoadContext(HtmlPostProcessContext context)
        {
            _registeredBookmarks =
                Deserialize<OSPlatformSensitiveDictionary<HashSet<string>>>(
                    context,
                    nameof(_registeredBookmarks));
            _fileMapping =
                Deserialize<OSPlatformSensitiveDictionary<string>>(
                    context,
                    nameof(_fileMapping));
        }

        protected override void HandleCore(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile)
        {
            _fileMapping[outputFile] = inputFile;

            // RFC 3986: relative-ref = relative-part [ "?" query ] [ "#" fragment ]
            _linksWithBookmark[outputFile] =
                (from node in GetNodesWithAttribute(document, "href")
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
                    string linkedToFile = linkItem.Href == string.Empty ? currentFile : linkItem.Href;
                    string bookmark = linkItem.Bookmark;
                    HashSet<string> bookmarks;
                    if (_registeredBookmarks.TryGetValue(linkedToFile, out bookmarks) && !bookmarks.Contains(bookmark))
                    {
                        string currentFileSrc = linkItem.SourceFile ?? _fileMapping[currentFile];
                        string linkedToFileSrc = _fileMapping[linkedToFile];
                        string link = linkItem.Href == string.Empty ? $"#{bookmark}" : $"{linkedToFileSrc}#{bookmark}";
                        string content = linkItem.SourceFragment;
                        if (string.IsNullOrEmpty(content))
                        {
                            // Invalid bookmarks introduced from templates is a corner case, ignored.
                            content = $"<a href=\"{link}\">{title}</a>";
                        }
                        Logger.LogWarning($"Illegal link: `{content}` -- missing bookmark. The file {linkedToFileSrc} doesn't contain a bookmark named {bookmark}.",
                            file: currentFileSrc,
                            line: linkItem.SourceLineNumber != 0 ? linkItem.SourceLineNumber.ToString() : null);
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
            return ((RelativePath)basePathFromRoot + relativePath).RemoveWorkingFolder();
        }

        private static T Deserialize<T>(HtmlPostProcessContext context, string name)
            where T : class, new()
        {
            return context.Load(
                name,
                stream =>
                {
                    using (var sr = new StreamReader(stream))
                    {
                        return JsonUtility.Deserialize<T>(sr);
                    }
                }) ?? new T();
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
