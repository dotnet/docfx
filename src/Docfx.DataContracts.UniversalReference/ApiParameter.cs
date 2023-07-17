// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.UniversalReference;

[Serializable]
public class ApiParameter
{
    [YamlMember(Alias = "id")]
    [JsonProperty("id")]
    [MergeOption(MergeOption.MergeKey)]
    public string Name { get; set; }

    /// <summary>
    /// parameter's types
    /// multiple types is allowed for a parameter in languages like JavaScript
    /// </summary>
    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    [UniqueIdentityReference]
    public List<string> Type { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    [MarkdownContent]
    public string Description { get; set; }

    [YamlMember(Alias = "optional")]
    [JsonProperty("optional")]
    public bool Optional { get; set; }

    [YamlMember(Alias = "defaultValue")]
    [JsonProperty("defaultValue")]
    public string DefaultValue { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
