// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Plugins;

public class OutputFileInfo
{
    [JsonProperty("relative_path")]
    [JsonPropertyName("relative_path")]
    public string RelativePath { get; set; }

    [JsonProperty("link_to_path")]
    [JsonPropertyName("link_to_path")]
    public string LinkToPath { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
