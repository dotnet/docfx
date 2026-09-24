// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiResponseViewModel
{
    [YamlMember(Alias = "schema")]
    [JsonProperty("schema", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("schema")]
    public RestApiSchemaViewModel Schema { get; set; }

    [YamlMember(Alias = "headers")]
    [JsonProperty("headers", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("headers")]
    public Dictionary<string, RestApiSchemaViewModel> Headers { get; set; }

    [YamlMember(Alias = "content")]
    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("content")]
    [Docfx.Common.EntityMergers.MergeOption(typeof(RestApiArrayMergeHandler))]
    public List<RestApiMediaTypeViewModel> Content { get; set; }

    [YamlMember(Alias = "statusCode")]
    [JsonProperty("statusCode")]
    [JsonPropertyName("statusCode")]
    [MergeOption(MergeOption.MergeKey)]
    public string HttpStatusCode { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [YamlMember(Alias = "examples")]
    [JsonProperty("examples")]
    [JsonPropertyName("examples")]
    public List<RestApiResponseExampleViewModel> Examples { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
