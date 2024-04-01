// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.Engine;

public class XRefMapRedirection
{
    [YamlMember(Alias = "uidPrefix")]
    [JsonProperty("uidPrefix")]
    [JsonPropertyName("uidPrefix")]
    public string UidPrefix { get; set; }

    [YamlMember(Alias = "href")]
    [JsonProperty("href")]
    [JsonPropertyName("href")]
    public string Href { get; set; }
}
