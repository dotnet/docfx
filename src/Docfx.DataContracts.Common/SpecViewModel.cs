// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.Common;

public class SpecViewModel
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    public string Uid { get; set; }

    [YamlMember(Alias = "name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [YamlMember(Alias = "isExternal")]
    [JsonPropertyName("isExternal")]
    public bool IsExternal { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Href)]
    [JsonPropertyName(Constants.PropertyName.Href)]
    public string Href { get; set; }
}
