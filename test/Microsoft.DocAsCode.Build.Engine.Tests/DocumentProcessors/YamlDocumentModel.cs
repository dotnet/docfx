// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.YamlSerialization;

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.Build.Engine.Tests;

[Serializable]
public class YamlDocumentModel
{
    [YamlMember(Alias = "documentType")]
    [JsonProperty("documentType")]
    public string DocumentType { get; set; }

    [ExtensibleMember]
    [JsonExtensionData]
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

    [YamlMember(Alias = "metadata")]
    [JsonProperty("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
