﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.Common;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.UniversalReference;

public class PageViewModel
{
    [YamlMember(Alias = "items")]
    [JsonProperty("items")]
    [JsonPropertyName("items")]
    public List<ItemViewModel> Items { get; set; } = new();

    [YamlMember(Alias = "references")]
    [JsonProperty("references")]
    [JsonPropertyName("references")]
    [UniqueIdentityReferenceIgnore]
    [MarkdownContentIgnore]
    public List<ReferenceViewModel> References { get; set; } = new();

    [YamlMember(Alias = "shouldSkipMarkup")]
    [JsonProperty("shouldSkipMarkup")]
    [JsonPropertyName("shouldSkipMarkup")]
    public bool ShouldSkipMarkup { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Metadata { get; set; } = new();
}
