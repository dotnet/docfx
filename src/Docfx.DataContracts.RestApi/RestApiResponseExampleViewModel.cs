// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Docfx.DataContracts.RestApi;

public class RestApiResponseExampleViewModel
{
    [YamlMember(Alias = "mimeType")]
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [YamlMember(Alias = "content")]
    [JsonPropertyName("content")]
    public string Content { get; set; }
}
