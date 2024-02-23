// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx.Plugins;

public class ManifestGroupInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("dest")]
    public string Destination { get; set; }

    [JsonPropertyName("xrefmap")]
    public string XRefmap { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

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
