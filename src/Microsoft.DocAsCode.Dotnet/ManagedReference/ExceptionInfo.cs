// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Common.EntityMergers;
using Microsoft.DocAsCode.DataContracts.Common;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.DataContracts.ManagedReference;

[Serializable]
public class ExceptionInfo
{
    [YamlMember(Alias = "type")]
    [MergeOption(MergeOption.MergeKey)]
    [JsonProperty("type")]
    [UniqueIdentityReference]
    public string Type { get; set; }

    [YamlMember(Alias = Constants.PropertyName.CommentId)]
    [JsonProperty(Constants.PropertyName.CommentId)]
    [MergeOption(MergeOption.Ignore)]
    public string CommentId { get; set; }

    [YamlMember(Alias = "description")]
    [JsonProperty("description")]
    [MarkdownContent]
    public string Description { get; set; }

    internal ExceptionInfo Clone()
    {
        return (ExceptionInfo)MemberwiseClone();
    }
}
