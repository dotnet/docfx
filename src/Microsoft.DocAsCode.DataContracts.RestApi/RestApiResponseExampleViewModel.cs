// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
