// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.YamlSerialization;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiParameterViewModel
{
    [YamlMember(Alias = "description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "name")]
    [JsonPropertyName("name")]
    [MergeOption(MergeOption.MergeKey)]
    public string Name { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
