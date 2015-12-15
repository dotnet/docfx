// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using YamlDotNet.Serialization;

    public class TemplateManifestItem
    {
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public string DocumentType { get; set; }
        [YamlMember(Alias = "original")]
        [JsonProperty("original")]
        public string OriginalFile { get; set; }
        [YamlMember(Alias = "output")]
        [JsonProperty("output")]
        public Dictionary<string, string> OutputFiles { get; set; }
    }
}
