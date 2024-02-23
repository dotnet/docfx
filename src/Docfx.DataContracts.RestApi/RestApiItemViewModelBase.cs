// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiItemViewModelBase : IOverwriteDocumentViewModel
{
    [YamlMember(Alias = Constants.PropertyName.Uid)]
    [JsonPropertyName(Constants.PropertyName.Uid)]
    [MergeOption(MergeOption.MergeKey)]
    public string Uid { get; set; }

    [YamlMember(Alias = "htmlId")]
    [JsonPropertyName("htmlId")]
    [MergeOption(MergeOption.Ignore)]
    public string HtmlId { get; set; }

    [YamlMember(Alias = "name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Conceptual)]
    [JsonPropertyName(Constants.PropertyName.Conceptual)]
    public string Conceptual { get; set; }

    [YamlMember(Alias = "description")]
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [YamlMember(Alias = "remarks")]
    [JsonPropertyName("remarks")]
    public string Remarks { get; set; }

    [YamlMember(Alias = "summary")]
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [YamlMember(Alias = Constants.PropertyName.Documentation)]
    [JsonPropertyName(Constants.PropertyName.Documentation)]
    [MergeOption(MergeOption.Ignore)]
    public SourceDetail Documentation { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
