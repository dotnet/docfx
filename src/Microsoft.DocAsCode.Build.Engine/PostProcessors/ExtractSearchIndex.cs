// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Composition;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Plugins;

    using HtmlAgilityPack;
    using Newtonsoft.Json;

    [Export(nameof(ExtractSearchIndex), typeof(IPostProcessor))]
    public class ExtractSearchIndex : IPostProcessor
    {
        private static readonly Regex RegexWhiteSpace = new Regex(@"\s+", RegexOptions.Compiled);

        public string Name => nameof(ExtractSearchIndex);
        public const string IndexFileName = "index.json";

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            if (!metadata.ContainsKey("_enableSearch"))
            {
                metadata = metadata.Add("_enableSearch", true);
            }
            return metadata;
        }

        public Manifest Process(Manifest manifest, string outputFolder)
        {
            if (outputFolder == null)
            {
                throw new ArgumentNullException("Base directory can not be null");
            }
            var indexData = new Dictionary<string, SearchIndexItem>();
            var indexDataFilePath = Path.Combine(outputFolder, IndexFileName);
            var htmlFiles = (from item in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                             from output in item.OutputFiles
                             where item.DocumentType != "Toc" && output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                             select output.Value.RelativePath).ToList();
            if (htmlFiles.Count == 0)
            {
                return manifest;
            }

            Logger.LogInfo($"Extracting index data from {htmlFiles.Count} html files");
            foreach (var relativePath in htmlFiles)
            {
                var filePath = Path.Combine(outputFolder, relativePath);
                var html = new HtmlDocument();
                Logger.LogDiagnostic($"Extracting index data from {filePath}");

                if (EnvironmentContext.FileAbstractLayer.Exists(filePath))
                {
                    try
                    {
                        using (var stream = EnvironmentContext.FileAbstractLayer.OpenRead(filePath))
                        {
                            html.Load(stream, Encoding.UTF8);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Warning: Can't load content from {filePath}: {ex.Message}");
                        continue;
                    }
                    var indexItem = ExtractItem(html, relativePath);
                    if (indexItem != null)
                    {
                        indexData[relativePath] = indexItem;
                    }
                }
            }
            JsonUtility.Serialize(indexDataFilePath, indexData, Formatting.Indented);

            // add index.json to mainfest as resource file
            var manifestItem = new ManifestItem
            {
                DocumentType = "Resource",
            };
            manifestItem.OutputFiles.Add("resource", new OutputFileInfo
            {
                RelativePath = PathUtility.MakeRelativePath(outputFolder, indexDataFilePath),
            });

            manifest.Files?.Add(manifestItem);
            return manifest;
        }

        internal SearchIndexItem ExtractItem(HtmlDocument html, string href)
        {
            var contentBuilder = new StringBuilder();

            if (html.DocumentNode.SelectNodes("/html/head/meta[@name='searchOption' and @content='noindex']") != null)
            {
                return null;
            }

            // Select content between the data-searchable class tag
            var nodes = html.DocumentNode.SelectNodes("//*[contains(@class,'data-searchable')]") ?? Enumerable.Empty<HtmlNode>();
            // Select content between the article tag
            nodes = nodes.Union(html.DocumentNode.SelectNodes("//article") ?? Enumerable.Empty<HtmlNode>());
            foreach (var node in nodes)
            {
                ExtractTextFromNode(node, contentBuilder);
            }

            var content = NormalizeContent(contentBuilder.ToString());
            var title = ExtractTitleFromHtml(html);

            return new SearchIndexItem { Href = href, Title = title, Keywords = content };
        }

        private string ExtractTitleFromHtml(HtmlDocument html)
        {
            var titleNode = html.DocumentNode.SelectSingleNode("//head/title");
            var originalTitle = titleNode?.InnerText;
            return NormalizeContent(originalTitle);
        }

        private string NormalizeContent(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            str = StringHelper.HtmlDecode(str);
            return RegexWhiteSpace.Replace(str, " ").Trim();
        }

        private void ExtractTextFromNode(HtmlNode root, StringBuilder contentBuilder)
        {
            if (root == null)
            {
                return;
            }

            if (!root.HasChildNodes)
            {
                contentBuilder.Append(root.InnerText);
                contentBuilder.Append(" ");
            }
            else
            {
                foreach (var node in root.ChildNodes)
                {
                    ExtractTextFromNode(node, contentBuilder);
                }
            }
        }
    }
}
