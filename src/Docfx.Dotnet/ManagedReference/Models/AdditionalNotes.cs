// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.DataContracts.Common;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

public class AdditionalNotes
{
    [YamlMember(Alias = "caller")]
    [JsonProperty("caller")]
    [JsonPropertyName("caller")]
    [MarkdownContent]
    public string Caller { get; set; }

    [YamlMember(Alias = "implementer")]
    [JsonProperty("implementer")]
    [JsonPropertyName("implementer")]
    [MarkdownContent]
    public string Implementer { get; set; }

    [YamlMember(Alias = "inheritor")]
    [JsonProperty("inheritor")]
    [JsonPropertyName("inheritor")]
    [MarkdownContent]
    public string Inheritor { get; set; }
}
