// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.UniversalReference;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.UniversalReference;

public class ApiLinkInfoBuildOutput
{
    [YamlMember(Alias = "linkType")]
    [JsonProperty("linkType")]
    [JsonPropertyName("linkType")]
    public LinkType LinkType { get; set; }

    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public ApiNames Type { get; set; }

    [YamlMember(Alias = "url")]
    [JsonProperty("url")]
    [JsonPropertyName("url")]
    public string Url { get; set; }
}
