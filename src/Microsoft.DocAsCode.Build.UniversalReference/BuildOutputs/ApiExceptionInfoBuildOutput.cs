// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.UniversalReference;

[Serializable]
public class ApiExceptionInfoBuildOutput
{
    [YamlMember(Alias = "type")]
    [JsonProperty("type")]
    public ApiNames Type { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    public string Description { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
