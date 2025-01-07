// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Plugins;

public class Manifest
{
    public Manifest() { }

    public Manifest(IEnumerable<ManifestItem> files) => Files.AddRange(files);

    [JsonProperty("sitemap")]
    [JsonPropertyName("sitemap")]
    public SitemapOptions Sitemap { get; set; }

    [JsonProperty("source_base_path")]
    [JsonPropertyName("source_base_path")]
    public string SourceBasePath { get; set; }

    [Obsolete]
    [JsonProperty("xrefmap")]
    [JsonPropertyName("xrefmap")]
    public object Xrefmap { get; set; }

    [JsonProperty("files")]
    [JsonPropertyName("files")]
    public List<ManifestItem> Files { get; init; } = [];

    [JsonProperty("groups")]
    [JsonPropertyName("groups")]
    public List<ManifestGroupInfo> Groups { get; set; }
}
