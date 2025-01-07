// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.UniversalReference;

public class ApiInheritanceTreeBuildOutput
{
    [YamlMember(Alias = Constants.PropertyName.Type)]
    [JsonProperty(Constants.PropertyName.Type)]
    [JsonPropertyName(Constants.PropertyName.Type)]
    public ApiNames Type { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Inheritance)]
    [JsonProperty(Constants.PropertyName.Inheritance)]
    [JsonPropertyName(Constants.PropertyName.Inheritance)]
    public List<ApiInheritanceTreeBuildOutput> Inheritance { get; set; }

    [YamlMember(Alias = "level")]
    [JsonProperty("level")]
    [JsonPropertyName("level")]
    public int Level { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
