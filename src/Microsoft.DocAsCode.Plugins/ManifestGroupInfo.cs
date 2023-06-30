// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Plugins;

public class ManifestGroupInfo
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("dest")]
    public string Destination { get; set; }

    [JsonProperty("xrefmap")]
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
