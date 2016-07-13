// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class OutputFileInfo
    {
        [YamlMember(Alias = "relative_path")]
        [JsonProperty("relative_path")]
        public string RelativePath { get; set; }

        [YamlMember(Alias = "link_to_path")]
        [JsonProperty("link_to_path")]
        public string LinkToPath { get; set; }

        [YamlMember(Alias = "hash")]
        [JsonProperty("hash")]
        public string Hash { get; set; }
    }
}
