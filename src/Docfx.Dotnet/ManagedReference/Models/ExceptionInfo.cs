// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.Common.EntityMergers;
using Docfx.DataContracts.Common;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

public class ExceptionInfo
{
    [YamlMember(Alias = "type")]
    [MergeOption(MergeOption.MergeKey)]
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    [UniqueIdentityReference]
    public string Type { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonProperty(Constants.PropertyName.CommentId)]
    [JsonPropertyName(Constants.PropertyName.CommentId)]
    [MergeOption(MergeOption.Ignore)]
    public string CommentId { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    [JsonPropertyName("description")]
    [MarkdownContent]
    public string Description { get; set; }

    internal ExceptionInfo Clone()
    {
        return (ExceptionInfo)MemberwiseClone();
    }
}
