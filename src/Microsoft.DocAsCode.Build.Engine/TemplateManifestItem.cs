// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization;

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
        [YamlMember(Alias = "hashes")]
        [JsonProperty("hashes")]
        public Dictionary<string, string> Hashes { get; set; }
        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
