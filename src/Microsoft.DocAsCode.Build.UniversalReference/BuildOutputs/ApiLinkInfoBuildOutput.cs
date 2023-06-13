// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.DataContracts.UniversalReference;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.UniversalReference;

[Serializable]
public class ApiLinkInfoBuildOutput
{
    [YamlMember(Alias = "linkType")]
    [JsonProperty("linkType")]
    public LinkType LinkType { get; set; }

    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    public ApiNames Type { get; set; }

    [YamlMember(Alias = "url")]
    [JsonProperty("url")]
    public string Url { get; set; }
}
