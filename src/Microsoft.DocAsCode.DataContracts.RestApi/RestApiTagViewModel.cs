// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Common.EntityMergers;
using Microsoft.DocAsCode.DataContracts.Common;
using Microsoft.DocAsCode.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.DataContracts.RestApi;

[Serializable]
public class RestApiTagViewModel : IOverwriteDocumentViewModel
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonProperty(Constants.PropertyName.Uid)]
    [MergeOption(MergeOption.MergeKey)]
    public string Uid { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Conceptual)]
    [JsonProperty(Constants.PropertyName.Conceptual)]
    public string Conceptual { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonProperty(Constants.PropertyName.Documentation)]
    [MergeOption(MergeOption.Ignore)]
    public SourceDetail Documentation { get; set; }

    [YamlMember(Alias = "name")]
    [JsonProperty("name")]
    public string Name { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "htmlId")]
    [JsonProperty("htmlId")]
    [MergeOption(MergeOption.Ignore)]
    public string HtmlId { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
