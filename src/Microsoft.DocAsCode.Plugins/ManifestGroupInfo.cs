// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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