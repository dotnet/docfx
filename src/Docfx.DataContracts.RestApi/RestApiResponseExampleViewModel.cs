// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiResponseExampleViewModel
{
    [YamlMember(Alias = "mimeType")]
    [JsonProperty("mimeType")]
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [YamlMember(Alias = "content")]
    [JsonProperty("content")]
    [JsonPropertyName("content")]
    public string Content { get; set; }
}
