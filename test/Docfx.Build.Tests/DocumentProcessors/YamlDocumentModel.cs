// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.Build.Engine.Tests;

[Serializable]
public class YamlDocumentModel
{
    [YamlMember(Alias = "documentType")]
    [JsonProperty("documentType")]
    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; }

    [ExtensibleMember]
    [Newtonsoft.Json.JsonExtensionData]
    [System.Text.Json.Serialization.JsonExtensionData]
    public Dictionary<string, object> Data { get; set; } = [];

    [YamlMember(Alias = "metadata")]
    [JsonProperty("metadata")]
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = [];
}
