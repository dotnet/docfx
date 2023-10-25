// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.UniversalReference;

public class ApiLanguageValuePairWithLevel<T> : ApiLanguageValuePair<T>
{
    [YamlMember(Alias = "level")]
    [JsonProperty("level")]
    [JsonPropertyName("level")]
    public int Level { get; set; }
}
