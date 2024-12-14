// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Plugins;

public class ManifestGroupInfo
{
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("dest")]
    [JsonPropertyName("dest")]
    public string Destination { get; set; }

    [JsonProperty("xrefmap")]
    [JsonPropertyName("xrefmap")]
    public string XRefmap { get; set; }

    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];

    // Default constructor for System.Text.Json deserialization
    public ManifestGroupInfo() { }

    public ManifestGroupInfo(GroupInfo groupInfo)
    {
        if (groupInfo == null)
        {
            return;
        }

        Name = groupInfo.Name;
        Destination = groupInfo.Destination;
        Metadata = groupInfo.Metadata;
    }
}
