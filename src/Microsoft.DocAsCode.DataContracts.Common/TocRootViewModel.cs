// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
