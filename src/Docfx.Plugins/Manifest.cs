// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx.Plugins;

public class Manifest
{
    public Manifest() { }

    public Manifest(IEnumerable<ManifestItem> files) => Files.AddRange(files);

    [JsonPropertyName("sitemap")]
    public SitemapOptions Sitemap { get; set; }

    [JsonPropertyName("source_base_path")]
    public string SourceBasePath { get; set; }

    [Obsolete]
    [JsonPropertyName("xrefmap")]
    public object Xrefmap { get; set; }

    [JsonPropertyName("files")]
    public List<ManifestItem> Files { get; } = new();

    [JsonPropertyName("groups")]
    public List<ManifestGroupInfo> Groups { get; set; }
}
