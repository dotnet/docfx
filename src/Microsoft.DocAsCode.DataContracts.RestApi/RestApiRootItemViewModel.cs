// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

using Microsoft.DocAsCode.Common.EntityMergers;

namespace Microsoft.DocAsCode.DataContracts.RestApi;

[Serializable]
public class RestApiRootItemViewModel : RestApiItemViewModelBase
{
    /// <summary>
    /// The original swagger.json content
    /// `_` prefix indicates that this metadata is generated
    /// </summary>
    [YamlMember(Alias = "_raw")]
    [JsonProperty("_raw")]
    [MergeOption(MergeOption.Ignore)]
    public string Raw { get; set; }

    [YamlMember(Alias = "tags")]
    [JsonProperty("tags")]
    public List<RestApiTagViewModel> Tags { get; set; }

    [YamlMember(Alias = "children")]
    [JsonProperty("children")]
    public List<RestApiChildItemViewModel> Children { get; set; }
}
