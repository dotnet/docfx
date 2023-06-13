// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Common.EntityMergers;
using Microsoft.DocAsCode.DataContracts.Common;
using Microsoft.DocAsCode.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.DataContracts.UniversalReference;

[Serializable]
public class LinkInfo
{
    [YamlMember(Alias = "linkType")]
    [JsonProperty("linkType")]
    [MergeOption(MergeOption.Ignore)]
    public LinkType LinkType { get; set; }

    [YamlMember(Alias = "linkId")]
    [MergeOption(MergeOption.MergeKey)]
    [JsonProperty("linkId")]
    public string LinkId { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonProperty(Constants.PropertyName.CommentId)]
    [MergeOption(MergeOption.Ignore)]
    public string CommentId { get; set; }

    [YamlMember(Alias = "altText")]
    [JsonProperty("altText")]
    public string AltText { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

[Serializable]
public enum LinkType
{
    CRef,
    HRef,
}
