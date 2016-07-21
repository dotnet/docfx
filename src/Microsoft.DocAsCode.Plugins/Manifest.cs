// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class Manifest
    {
        [YamlMember(Alias = "templates")]
        [JsonProperty("templates")]
        public List<string> Templates { get; set; }

        [YamlMember(Alias = "homepages")]
        [JsonProperty("homepages")]
        public List<HomepageInfo> Homepages { get; set; }

        [YamlMember(Alias = "sourceBasePath")]
        [JsonProperty("source_base_path")]
        public string SourceBasePath { get; set; }

        [YamlMember(Alias = "xrefmap")]
        [JsonProperty("xrefmap")]
        public object XRefMap { get; set; }

        [YamlMember(Alias = "files")]
        [JsonProperty("files")]
        public List<ManifestItem> Files { get; set; }
    }
}
