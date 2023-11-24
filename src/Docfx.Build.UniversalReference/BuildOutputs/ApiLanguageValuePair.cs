// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.UniversalReference;

public class ApiLanguageValuePair<T>
{
    [YamlMember(Alias = "lang")]
    [JsonProperty("lang")]
    [JsonPropertyName("lang")]
    public string Language { get; set; }

    [YamlMember(Alias = "value")]
    [JsonProperty("value")]
    [JsonPropertyName("value")]
    public T Value { get; set; }
}
