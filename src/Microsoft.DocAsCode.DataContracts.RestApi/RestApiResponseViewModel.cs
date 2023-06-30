// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Common.EntityMergers;
using Microsoft.DocAsCode.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.DataContracts.RestApi;

[Serializable]
public class RestApiResponseViewModel
{
    [YamlMember(Alias = "statusCode")]
    [JsonProperty("statusCode")]
    [MergeOption(MergeOption.MergeKey)]
    public string HttpStatusCode { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonProperty("summary")]
    public string Summary { get; set; }

    [YamlMember(Alias = "examples")]
    [JsonProperty("examples")]
    public List<RestApiResponseExampleViewModel> Examples { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
