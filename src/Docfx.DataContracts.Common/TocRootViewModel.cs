// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.Common;

public class TocRootViewModel
{
    [YamlMember(Alias = "items")]
    [JsonProperty("items")]
    [JsonPropertyName("items")]
    public TocViewModel Items { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
