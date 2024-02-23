// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.YamlSerialization;

using YamlDotNet.Serialization;

namespace Docfx.Build.UniversalReference;

public class ApiExceptionInfoBuildOutput
{
    [YamlMember(Alias = "type")]
    [JsonPropertyName("type")]
    public ApiNames Type { get; set; }

    [YamlMember(Alias = "description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
