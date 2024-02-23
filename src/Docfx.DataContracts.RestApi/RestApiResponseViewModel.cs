// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.YamlSerialization;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiResponseViewModel
{
    [YamlMember(Alias = "statusCode")]
    [JsonPropertyName("statusCode")]
    [MergeOption(MergeOption.MergeKey)]
    public string HttpStatusCode { get; set; }

    [YamlMember(Alias = "description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [YamlMember(Alias = "examples")]
    [JsonPropertyName("examples")]
    public List<RestApiResponseExampleViewModel> Examples { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
