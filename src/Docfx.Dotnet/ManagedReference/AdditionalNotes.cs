// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.Common;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.ManagedReference;

[Serializable]
public class AdditionalNotes
{
    [JsonProperty("caller")]
    [YamlMember(Alias = "caller")]
    [MarkdownContent]
    public string Caller { get; set; }

    [JsonProperty("implementer")]
    [YamlMember(Alias = "implementer")]
    [MarkdownContent]
    public string Implementer { get; set; }

    [JsonProperty("inheritor")]
    [YamlMember(Alias = "inheritor")]
    [MarkdownContent]
    public string Inheritor { get; set; }
}
