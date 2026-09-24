// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiSchemaCompositionViewModel
{
    [YamlMember(Alias = "kind")]
    [JsonProperty("kind", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("kind")]
    public string Kind { get; set; }

    [YamlMember(Alias = "schemas")]
    [JsonProperty("schemas", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("schemas")]
    [Docfx.Common.EntityMergers.MergeOption(typeof(RestApiArrayMergeHandler))]
    public List<RestApiSchemaViewModel> Schemas { get; set; }
}
