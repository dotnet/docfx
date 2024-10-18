// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Plugins;
using HtmlAgilityPack;

namespace Docfx.Build.Common;

public class YamlHtmlPart
{
    public MarkupResult Origin { get; set; }

    public string Html { get; set; }

    public string Conceptual { get; set; }

    public string SourceFile { get; set; }

    public int StartLine { get; set; }

    public int EndLine { get; set; }

    public ImmutableArray<string> LinkToFiles { get; set; } = [];

    public ImmutableHashSet<string> LinkToUids { get; set; } = [];

    public ImmutableDictionary<string, object> YamlHeader { get; set; } = ImmutableDictionary<string, object>.Empty;

    public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> UidLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;

    public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> FileLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;

    public MarkupResult ToMarkupResult()
    {
        return new MarkupResult
        {
            Html = Html,
            Dependency = Origin.Dependency,
            LinkToFiles = LinkToFiles,
            LinkToUids = LinkToUids,
            YamlHeader = YamlHeader,
            FileLinkSources = FileLinkSources,
            UidLinkSources = UidLinkSources,
        };
    }

    public static IList<YamlHtmlPart> SplitYamlHtml(MarkupResult origin)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(origin.Html);
        var parts = new List<YamlHtmlPart>();

        var nodes = doc.DocumentNode.SelectNodes("//yamlheader");
        if (nodes == null) return parts;

        foreach (var node in nodes)
        {
            var sourceFile = node.GetAttributeValue("sourceFile", "NotFound");
            var startLine = node.GetAttributeValue("start", -1);
            var endLine = node.GetAttributeValue("end", -1);

            parts.Add(new YamlHtmlPart
            {
                Origin = origin,
                SourceFile = sourceFile,
                StartLine = startLine,
                EndLine = endLine
            });
        }

        var startIndexes = nodes.Select(node => node.StreamPosition).Skip(1).ToList();
        startIndexes.Add(origin.Html.Length);
        var endIndexes = nodes.Select(node => node.StreamPosition + node.OuterHtml.Length - 1).ToList();

        for (var i = 0; i < parts.Count; i++)
        {
            if (i == 0)
            {
                parts[i].Html = origin.Html.Substring(0, startIndexes[0]);
            }
            else
            {
                parts[i].Html = origin.Html.Substring(startIndexes[i - 1], startIndexes[i] - startIndexes[i - 1]);
            }
            parts[i].Conceptual = origin.Html.Substring(endIndexes[i] + 1, startIndexes[i] - endIndexes[i] - 1);
        }

        return parts;
    }
}
