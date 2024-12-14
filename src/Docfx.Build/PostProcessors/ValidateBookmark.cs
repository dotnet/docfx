// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Web;
using Docfx.Common;
using Docfx.Plugins;
using HtmlAgilityPack;

namespace Docfx.Build.Engine;

sealed class ValidateBookmark : HtmlDocumentHandler
{
    private static readonly string XPathTemplate = "//*/@{0}";
    private static readonly HashSet<string> WhiteList = ["top"];
    /// <summary>
    /// bookmarks mapping from output file -> bookmarks
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _registeredBookmarks = new(FilePathComparer.OSPlatformSensitiveStringComparer);
    /// <summary>
    /// file mapping from output file -> src file
    /// </summary>
    private readonly Dictionary<string, string> _fileMapping = new(FilePathComparer.OSPlatformSensitiveStringComparer);
    private readonly Dictionary<string, List<LinkItem>> _linksWithBookmark = new(FilePathComparer.OSPlatformSensitiveStringComparer);

    #region IHtmlDocumentHandler members

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
                 Bookmark = Uri.UnescapeDataString(bookmark),
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
