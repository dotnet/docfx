// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiSchemaViewModel
{
    [YamlMember(Alias = "type")]
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [YamlMember(Alias = "format")]
    [JsonProperty("format", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("format")]
    public string Format { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "x-internal-ref-name")]
    [JsonProperty("x-internal-ref-name", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("x-internal-ref-name")]
    public string ReferenceName { get; set; }

    [YamlMember(Alias = "x-internal-loop-ref-name")]
    [JsonProperty("x-internal-loop-ref-name", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("x-internal-loop-ref-name")]
    public string LoopReferenceName { get; set; }

    [YamlMember(Alias = "properties")]
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("properties")]
    public Dictionary<string, RestApiSchemaViewModel> Properties { get; set; }

    [YamlMember(Alias = "items")]
    [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("items")]
    public RestApiSchemaViewModel Items { get; set; }

    [YamlMember(Alias = "allOf")]
    [JsonProperty("allOf", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("allOf")]
    [MergeOption(typeof(RestApiArrayMergeHandler))]
    public List<RestApiSchemaViewModel> AllOf { get; set; }

    [YamlMember(Alias = "required")]
    [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("required")]
    public object Required { get; set; }

    [YamlMember(Alias = "enum")]
    [JsonProperty("enum", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("enum")]
    [MergeOption(typeof(RestApiArrayMergeHandler))]
    public List<object> Enum { get; set; }

    [YamlMember(Alias = "example")]
    [JsonProperty("example", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("example")]
    public object Example { get; set; }

    [Docfx.YamlSerialization.ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
