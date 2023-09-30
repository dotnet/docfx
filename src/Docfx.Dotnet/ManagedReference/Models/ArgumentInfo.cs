// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.Common;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

public class ArgumentInfo
{
    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    [UniqueIdentityReference]
    public string Type { get; set; }

    [YamlMember(Alias = "value")]
    [JsonProperty("value")]
    public object Value { get; set; }
}
