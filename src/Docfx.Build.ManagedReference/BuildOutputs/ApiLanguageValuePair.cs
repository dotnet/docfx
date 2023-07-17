// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.ManagedReference.BuildOutputs;

[Serializable]
public class ApiLanguageValuePair
{
    [YamlMember(Alias = "lang")]
    [JsonProperty("lang")]
    public string Language { get; set; }

    [YamlMember(Alias = "value")]
    [JsonProperty("value")]
    public string Value { get; set; }
}
