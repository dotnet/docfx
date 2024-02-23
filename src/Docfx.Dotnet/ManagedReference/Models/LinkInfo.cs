// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

public class LinkInfo
{
    [YamlMember(Alias = "linkType")]
    [JsonPropertyName("linkType")]
    [MergeOption(MergeOption.Ignore)]
    public LinkType LinkType { get; set; }

    [YamlMember(Alias = "linkId")]
    [MergeOption(MergeOption.MergeKey)]
    [JsonPropertyName("linkId")]
    public string LinkId { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    [MergeOption(MergeOption.Ignore)]
    public string CommentId { get; set; }

    [YamlMember(Alias = "altText")]
    [JsonPropertyName("altText")]
    public string AltText { get; set; }

    internal LinkInfo Clone()
    {
        return (LinkInfo)MemberwiseClone();
    }
}

public enum LinkType
{
    CRef,
    HRef,
}
