// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiParameterViewModel
{
    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "name")]
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    [MergeOption(MergeOption.MergeKey)]
    public string Name { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
