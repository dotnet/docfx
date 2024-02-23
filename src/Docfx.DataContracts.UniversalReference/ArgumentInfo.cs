// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using YamlDotNet.Serialization;

namespace Docfx.DataContracts.UniversalReference;

public class ArgumentInfo
{
    [YamlMember(Alias = "type")]
    [JsonPropertyName("type")]
    [UniqueIdentityReference]
    public string Type { get; set; }

    [YamlMember(Alias = "value")]
    [JsonPropertyName("value")]
    public object Value { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
