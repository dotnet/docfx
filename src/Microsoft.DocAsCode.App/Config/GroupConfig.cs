// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.DocAsCode;

[Serializable]
internal class GroupConfig
{
    [JsonProperty("dest")]
    public string Destination { get; set; }

    [JsonProperty("xrefTags")]
    public ListWithStringFallback XrefTags { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
