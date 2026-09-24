// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiMediaTypeViewModel
{
    [YamlMember(Alias = "mimeType")]
    [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [YamlMember(Alias = "schema")]
    [JsonProperty("schema", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("schema")]
    public RestApiSchemaViewModel Schema { get; set; }

    [YamlMember(Alias = "itemSchema")]
    [JsonProperty("itemSchema", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("itemSchema")]
    public RestApiSchemaViewModel ItemSchema { get; set; }

    [YamlMember(Alias = "examples")]
    [JsonProperty("examples", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("examples")]
    [Docfx.Common.EntityMergers.MergeOption(typeof(RestApiArrayMergeHandler))]
    public List<RestApiResponseExampleViewModel> Examples { get; set; }
}
