// Copyright (c) Microsoft. All rights reserved.
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

    public class ValidateBookmark : IHtmlDocumentHandler
    {
        private static readonly string XpathTemplate = "//*/@{0}";
        private static readonly HashSet<string> WhiteList = new HashSet<string> { "top" };
        private Dictionary<string, HashSet<string>> registeredBookmarks;
        private Dictionary<string, List<Tuple<string, string>>> bookmarks;
        private Dictionary<string, string> fileMapping;

        #region IHtml

        public Manifest PreHandle(Manifest manifest)
        {
            registeredBookmarks = new Dictionary<string, HashSet<string>>(new FilePathComparer());
            bookmarks = new Dictionary<string, List<Tuple<string, string>>>(new FilePathComparer());
            fileMapping = new Dictionary<string, string>(new FilePathComparer());
            return manifest;
        }

        public void Handle(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile)
        {
            fileMapping[outputFile] = inputFile;
            bookmarks[outputFile] =
                (from link in GetNodeAttribute(document, "src").Concat(GetNodeAttribute(document, "href"))
                 let index = link.IndexOf("#")
                 where index != -1 && PathUtility.IsRelativePath(link)
                 select Tuple.Create(HttpUtility.UrlDecode(link.Remove(index)), link.Substring(index + 1)) into pair
                 where !WhiteList.Contains(pair.Item2)
                 select pair).ToList();
            var anchors = GetNodeAttribute(document, "id").Concat(GetNodeAttribute(document, "name"));
            registeredBookmarks[outputFile] = new HashSet<string>(anchors);
        }

        public Manifest PostHandle(Manifest manifest)
        {
            foreach (var item in bookmarks)
            {
                string path = item.Key;
                foreach (var b in item.Value)
                {
                    string linkedToFile = b.Item1 == string.Empty ? path : b.Item1;
                    string anchor = b.Item2;
                    HashSet<string> anchors;
                    if (registeredBookmarks.TryGetValue(linkedToFile, out anchors) && !anchors.Contains(anchor))
                    {
                        string currentFileSrc = fileMapping[path];
                        string linkedToFileSrc = fileMapping[linkedToFile];
                        Logger.LogWarning($"Output file {path} which is built from src file {currentFileSrc} contains illegal link {linkedToFile}#{anchor}: the file {linkedToFile} which is built from src {linkedToFileSrc} doesn't contain a bookmark named {anchor}.");
                    }
                }
            }
            return manifest;
        }

        #endregion

        private static IEnumerable<string> GetNodeAttribute(HtmlDocument html, string attribute)
        {
            var nodes = html.DocumentNode.SelectNodes(string.Format(XpathTemplate, attribute));
            if (nodes == null)
            {
                return Enumerable.Empty<string>();
            }
            return nodes.Select(n => n.GetAttributeValue(attribute, null));
        }
    }
}
