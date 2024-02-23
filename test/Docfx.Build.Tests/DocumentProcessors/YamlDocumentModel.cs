// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Docfx.YamlSerialization;

using YamlDotNet.Serialization;

namespace Docfx.Build.Engine.Tests;

[Serializable]
public class YamlDocumentModel
{
    [YamlMember(Alias = "documentType")]
    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

    [YamlMember(Alias = "metadata")]
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
