// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.RestApi
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

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
}
