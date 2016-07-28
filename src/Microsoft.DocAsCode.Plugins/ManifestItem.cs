// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.YamlSerialization;

    public class ManifestItem
    {
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public string DocumentType { get; set; }

        [YamlMember(Alias = "sourceRelativePath")]
        [JsonProperty("source_relative_path")]
        public string SourceRelativePath { get; set; }

        [YamlMember(Alias = "output")]
        [JsonProperty("output")]
        public Dictionary<string, OutputFileInfo> OutputFiles { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
