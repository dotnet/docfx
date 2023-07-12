// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

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

    internal LinkInfo Clone()
    {
        return (LinkInfo)MemberwiseClone();
    }
}

[Serializable]
public enum LinkType
{
    CRef,
    HRef,
}
