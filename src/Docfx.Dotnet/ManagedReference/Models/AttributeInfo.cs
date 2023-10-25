// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.Common;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

public class AttributeInfo
{
    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    [UniqueIdentityReference]
    public string Type { get; set; }

    [YamlMember(Alias = "ctor")]
    [JsonProperty("ctor")]
    [JsonPropertyName("ctor")]
    public string Constructor { get; set; }

    [YamlMember(Alias = "arguments")]
    [JsonProperty("arguments")]
    [JsonPropertyName("arguments")]
    public List<ArgumentInfo> Arguments { get; set; }

    [YamlMember(Alias = "namedArguments")]
    [JsonProperty("namedArguments")]
    [JsonPropertyName("namedArguments")]
    public List<NamedArgumentInfo> NamedArguments { get; set; }
}
