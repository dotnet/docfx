// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

public class ApiParameter
{
    [YamlMember(Alias = "id")]
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    [MergeOption(MergeOption.MergeKey)]
    public string Name { get; set; }

    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    [UniqueIdentityReference]
    public string Type { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    [MarkdownContent]
    public string Description { get; set; }

    [YamlMember(Alias = "attributes")]
    [JsonProperty("attributes")]
    [JsonPropertyName("attributes")]
    [MergeOption(MergeOption.Ignore)]
    public List<AttributeInfo> Attributes { get; set; }
}
