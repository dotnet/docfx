// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx.Plugins;

public class ManifestItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("source_relative_path")]
    public string SourceRelativePath { get; set; }

    [JsonPropertyName("output")]
    public Dictionary<string, OutputFileInfo> Output { get; } = new();

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("group")]
    public string Group { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
