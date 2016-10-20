﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;
    using HtmlAgilityPack;

    [Export(nameof(ValidateBookmark), typeof(IPostProcessor))]
    public class ValidateBookmark : IPostProcessor
    {
        private static readonly string XpathTemplate = "//*/@{0}";
        private static readonly HashSet<string> WhiteList = new HashSet<string> { "top" };

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            return metadata;
        }

        public Manifest Process(Manifest manifest, string outputFolder)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (outputFolder == null)
            {
                throw new ArgumentNullException("Base directory can not be null");
            }
            var registeredBookmarks = new Dictionary<string, HashSet<string>>(new FilePathComparer());
            var bookmarks = new Dictionary<string, List<Tuple<string, string>>>(new FilePathComparer());
            var fileMapping = new Dictionary<string, string>(new FilePathComparer());

            foreach (var p in from item in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                              from output in item.OutputFiles
                              where output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                              select new
                              {
                                  RelativePath = output.Value.RelativePath,
                                  SrcRelativePath = item.SourceRelativePath,
                              })
            {
                string srcRelativePath = p.SrcRelativePath;
                string relativePath = p.RelativePath;
                var filePath = Path.Combine(outputFolder, relativePath);

                var html = new HtmlDocument();

                if (File.Exists(filePath))
                {
                    try
                    {
                        html.Load(filePath, Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Warning: Can't load content from {filePath}: {ex.Message}");
                        continue;
                    }
                    fileMapping[relativePath] = srcRelativePath;
                    var links = GetNodeAttribute(html, "src").Concat(GetNodeAttribute(html, "href"));
                    bookmarks[relativePath] = (from link in links
                                               let index = link.IndexOf("#")
                                               where index != -1 && PathUtility.IsRelativePath(link) && !WhiteList.Contains(link.Substring(index + 1))
                                               select Tuple.Create(
                                                   HttpUtility.UrlDecode(link.Remove(index)),
                                                   link.Substring(index + 1))).ToList();
                    var anchors = GetNodeAttribute(html, "id").Concat(GetNodeAttribute(html, "name"));
                    registeredBookmarks[relativePath] = new HashSet<string>(anchors);
                }
            }

            // validate bookmarks
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
