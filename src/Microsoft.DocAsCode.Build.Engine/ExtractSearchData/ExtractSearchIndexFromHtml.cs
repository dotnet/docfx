// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// todo : move to Plugin

namespace Microsoft.DocAsCode.Build.Engine.ExtractSearchData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.IO;

    using Newtonsoft.Json;
    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class ExtractSearchIndexFromHtml
    {
        public const string IndexFileName = "index.json";

        private static readonly Regex RegexWhiteSpace = new Regex(@"\s+", RegexOptions.Compiled);

        public static void GenerateFile(List<TemplateManifestItem> manifest, string baseDir)
        {
            if (baseDir == null)
            {
                throw new ArgumentNullException("Base directory can not be null");
            }
            var indexData = new Dictionary<string, SearchIndexItem>();
            var indexDataFilePath = Path.Combine(baseDir, IndexFileName);
            var htmlFiles = (from item in manifest ?? Enumerable.Empty<TemplateManifestItem>()
                             from output in item.OutputFiles
                             where output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                             select output.Value).ToList();
            if (htmlFiles.Count == 0)
            {
                return;
            }

            Logger.LogInfo($"Extracting index data from {htmlFiles.Count} html files");
            foreach (var relativePath in htmlFiles)
            {
                var filePath = Path.Combine(baseDir, relativePath);
                var html = new HtmlDocument();
                Logger.LogVerbose($"Extracting index data from {filePath}");

                if (File.Exists(filePath))
                {
                    try
                    {
                        html.Load(filePath);
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
        }

        public static SearchIndexItem ExtractItem(HtmlDocument html, string href)
        {
            var contentBuilder = new StringBuilder();

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

        private static string ExtractTitleFromHtml(HtmlDocument html)
        {
            var titleNode = html.DocumentNode.SelectSingleNode("//head/title");
            return titleNode?.InnerText ?? string.Empty;
        }

        private static string NormalizeContent(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            str = StringHelper.HtmlDecode(str);
            return RegexWhiteSpace.Replace(str, " ").Trim();
        }

        private static void ExtractTextFromNode(HtmlNode root, StringBuilder contentBuilder)
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
