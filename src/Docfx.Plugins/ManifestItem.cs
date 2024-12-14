// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Plugins;

public class ManifestItem
{
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonProperty("source_relative_path")]
    [JsonPropertyName("source_relative_path")]
    public string SourceRelativePath { get; set; }

    [JsonProperty("output")]
    [JsonPropertyName("output")]
    public Dictionary<string, OutputFileInfo> Output { get; init; } = [];

    [JsonProperty("version")]
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonProperty("group")]
    [JsonPropertyName("group")]
    public string Group { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
