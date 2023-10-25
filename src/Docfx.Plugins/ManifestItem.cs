// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx.Plugins;

public class ManifestItem
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("source_relative_path")]
    public string SourceRelativePath { get; set; }

    [JsonProperty("output")]
    public OutputFileCollection Output { get; } = new OutputFileCollection();

    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("group")]
    public string Group { get; set; }

    [JsonProperty("log_codes")]
    public ICollection<string> LogCodes;

    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    public ManifestItem Clone()
    {
        return (ManifestItem)MemberwiseClone();
    }
}
