// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Docfx.Common;
using Docfx.Plugins;
using HtmlAgilityPack;

namespace Docfx.Build.Engine;

[Export(nameof(ExtractSearchIndex), typeof(IPostProcessor))]
partial class ExtractSearchIndex : IPostProcessor
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex s_regexWhiteSpace();

    private static readonly HashSet<string> s_htmlInlineTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "area", "del", "ins", "link", "map", "meta", "abbr", "audio", "b", "bdo", "button", "canvas", "cite", "code", "command", "data",
        "datalist", "dfn", "em", "embed", "i", "iframe", "img", "input", "kbd", "keygen", "label", "mark", "math", "meter", "noscript", "object",
        "output", "picture", "progress", "q", "ruby", "samp", "script", "select", "small", "span", "strong", "sub", "sup", "svg", "textarea", "time",
        "var", "video", "wbr",
    };

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

    public Manifest Process(Manifest manifest, string outputFolder, CancellationToken cancellationToken = default)
    {
        if (outputFolder == null)
        {
            throw new ArgumentNullException(nameof(outputFolder), "Base directory can not be null");
        }
        var indexData = new SortedDictionary<string, SearchIndexItem>();
        var indexDataFilePath = Path.Combine(outputFolder, IndexFileName);
        var htmlFiles = (from item in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                         from output in item.Output
                         where item.Type != "Toc" && output.Key.Equals(".html", StringComparison.OrdinalIgnoreCase)
                         select output.Value.RelativePath).ToList();
        if (htmlFiles.Count == 0)
        {
            return manifest;
        }

        Logger.LogInfo($"Extracting index data from {htmlFiles.Count} html files");
        foreach (var relativePath in htmlFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = Path.Combine(outputFolder, relativePath);
            var html = new HtmlDocument();
            Logger.LogDiagnostic($"Extracting index data from {filePath}");

            if (EnvironmentContext.FileAbstractLayer.Exists(filePath))
            {
                try
                {
                    using var stream = EnvironmentContext.FileAbstractLayer.OpenRead(filePath);
                    html.Load(stream, Encoding.UTF8);
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
        JsonUtility.Serialize(indexDataFilePath, indexData, indented: true);

        // add index.json to manifest as resource file
        var manifestItem = new ManifestItem
        {
            Type = "Resource",
        };
        manifestItem.Output.Add("resource", new OutputFileInfo
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

    private static string ExtractTitleFromHtml(HtmlDocument html)
    {
        var titleNode = html.DocumentNode.SelectSingleNode("//head/title");
        var originalTitle = titleNode?.InnerText;
        return NormalizeContent(originalTitle);
    }

    private static string NormalizeContent(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }
        str = WebUtility.HtmlDecode(str);
        return s_regexWhiteSpace().Replace(str, " ").Trim();
    }

    private static void ExtractTextFromNode(HtmlNode node, StringBuilder contentBuilder)
    {
        if (node == null)
        {
            return;
        }

        if (node.NodeType is HtmlNodeType.Text or HtmlNodeType.Comment)
        {
            contentBuilder.Append(node.InnerText);
            return;
        }

        if (node.NodeType is HtmlNodeType.Element or HtmlNodeType.Document)
        {
            var isBlock = !s_htmlInlineTags.Contains(node.Name);
            if (isBlock)
                contentBuilder.Append(' ');

            foreach (var childNode in node.ChildNodes)
                ExtractTextFromNode(childNode, contentBuilder);

            if (isBlock)
                contentBuilder.Append(' ');
        }
    }
}
