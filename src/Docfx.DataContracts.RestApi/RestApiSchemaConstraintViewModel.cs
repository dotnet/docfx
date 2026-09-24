// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiSchemaConstraintViewModel
{
    [YamlMember(Alias = "name")]
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [YamlMember(Alias = "value")]
    [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("value")]
    public string Value { get; set; }
}
