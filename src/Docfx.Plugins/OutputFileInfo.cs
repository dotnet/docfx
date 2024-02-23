// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx.Plugins;

public class OutputFileInfo
{
    [JsonPropertyName("relative_path")]
    public string RelativePath { get; set; }

    [JsonPropertyName("link_to_path")]
    public string LinkToPath { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
