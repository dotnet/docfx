// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiSecuritySchemeViewModel
{
    [YamlMember(Alias = "description")]
    [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [Docfx.YamlSerialization.ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
