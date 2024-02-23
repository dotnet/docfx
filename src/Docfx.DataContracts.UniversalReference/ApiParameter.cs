// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using YamlDotNet.Serialization;

namespace Docfx.DataContracts.UniversalReference;

public class ApiParameter
{
    [YamlMember(Alias = "id")]
    [JsonPropertyName("id")]
    [MergeOption(MergeOption.MergeKey)]
    public string Name { get; set; }

    /// <summary>
    /// parameter's types
    /// multiple types is allowed for a parameter in languages like JavaScript
    /// </summary>
    [YamlMember(Alias = "type")]
    [JsonPropertyName("type")]
    [UniqueIdentityReference]
    public List<string> Type { get; set; }

    [YamlMember(Alias = "description")]
    [JsonPropertyName("description")]
    [MarkdownContent]
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
