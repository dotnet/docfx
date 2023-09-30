// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.UniversalReference;

public class PageViewModel
{
    [YamlMember(Alias = "items")]
    [JsonProperty("items")]
    public List<ItemViewModel> Items { get; set; } = new List<ItemViewModel>();

    [YamlMember(Alias = "references")]
    [JsonProperty("references")]
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public List<ReferenceViewModel> References { get; set; } = new List<ReferenceViewModel>();

    [YamlMember(Alias = "shouldSkipMarkup")]
    [JsonProperty("shouldSkipMarkup")]
    public bool ShouldSkipMarkup { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
