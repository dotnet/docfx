// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

public class PageViewModel
{
    [YamlMember(Alias = "items")]
    [JsonProperty("items")]
    [JsonPropertyName("items")]
    public List<ItemViewModel> Items { get; set; } = [];

    [YamlMember(Alias = "references")]
    [JsonProperty("references")]
    [JsonPropertyName("references")]
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public List<ReferenceViewModel> References { get; set; } = [];

    [YamlMember(Alias = "shouldSkipMarkup")]
    [JsonProperty("shouldSkipMarkup")]
    [JsonPropertyName("shouldSkipMarkup")]
    public bool ShouldSkipMarkup { get; set; }

    [YamlMember(Alias = "memberLayout")]
    [JsonProperty("memberLayout")]
    [JsonPropertyName("memberLayout")]
    public MemberLayout MemberLayout { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
