// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.YamlSerialization;

using YamlDotNet.Serialization;

namespace Docfx.Build.UniversalReference;

public class ApiParameterBuildOutput
{
    [YamlMember(Alias = "id")]
    [JsonPropertyName("id")]
    public string Name { get; set; }

    [YamlMember(Alias = "type")]
    [JsonPropertyName("type")]
    public List<ApiNames> Type { get; set; }

    [YamlMember(Alias = "description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "optional")]
    [JsonPropertyName("optional")]
    public bool Optional { get; set; }

    [YamlMember(Alias = "defaultValue")]
    [JsonPropertyName("defaultValue")]
    public string DefaultValue { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
