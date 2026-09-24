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
    [YamlMember(Alias = "schema")]
    [JsonProperty("schema", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("schema")]
    public RestApiSchemaViewModel Schema { get; set; }

    [YamlMember(Alias = "content")]
    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("content")]
    [Docfx.Common.EntityMergers.MergeOption(typeof(RestApiArrayMergeHandler))]
    public List<RestApiMediaTypeViewModel> Content { get; set; }

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
