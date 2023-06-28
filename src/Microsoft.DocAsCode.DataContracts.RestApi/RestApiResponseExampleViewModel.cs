// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Microsoft.DocAsCode.DataContracts.RestApi;

[Serializable]
public class RestApiResponseExampleViewModel
{
    [YamlMember(Alias = "mimeType")]
    [JsonProperty("mimeType")]
    public string MimeType { get; set; }

    [YamlMember(Alias = "content")]
    [JsonProperty("content")]
    public string Content { get; set; }
}
