// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiRequestBodyViewModel
{
    [YamlMember(Alias = "description")]
    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "required")]
    [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    [YamlMember(Alias = "content")]
    [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("content")]
    [Docfx.Common.EntityMergers.MergeOption(typeof(RestApiArrayMergeHandler))]
    public List<RestApiMediaTypeViewModel> Content { get; set; }
}
