// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

using Microsoft.DocAsCode.YamlSerialization;

namespace Microsoft.DocAsCode.DataContracts.Common;

[Serializable]
public class TocRootViewModel
{
    [YamlMember(Alias = "items")]
    [JsonProperty("items")]
    public TocViewModel Items { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
