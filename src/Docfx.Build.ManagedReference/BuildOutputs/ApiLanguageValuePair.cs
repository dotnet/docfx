// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Docfx.Build.ManagedReference.BuildOutputs;

public class ApiLanguageValuePair
{
    [YamlMember(Alias = "lang")]
    [JsonPropertyName("lang")]
    public string Language { get; set; }

    [YamlMember(Alias = "value")]
    [JsonPropertyName("value")]
    public string Value { get; set; }
}
