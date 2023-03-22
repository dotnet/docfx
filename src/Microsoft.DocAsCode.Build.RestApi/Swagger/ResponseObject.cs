// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

using Microsoft.DocAsCode.YamlSerialization;

namespace Microsoft.DocAsCode.Build.RestApi.Swagger;

[Serializable]
public class ResponseObject
{
    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    public string Summary { get; set; }

    /// <summary>
    /// Key is the mime type
    /// </summary>
    [YamlMember(Alias = "examples")]
    [JsonProperty("examples")]
    public Dictionary<string, object> Examples { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
