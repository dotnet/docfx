// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.UniversalReference;

[Serializable]
public class ApiParameterBuildOutput
{
    [YamlMember(Alias = "id")]
    [JsonProperty("id")]
    public string Name { get; set; }

    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    public List<ApiNames> Type { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "optional")]
    [JsonProperty("optional")]
    public bool Optional { get; set; }

    [YamlMember(Alias = "defaultValue")]
    [JsonProperty("defaultValue")]
    public string DefaultValue { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
