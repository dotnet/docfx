// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Builders
{
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class ManifestItem
    {
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public string DocumentType { get; set; }
        [YamlMember(Alias = "model")]
        [JsonProperty("model")]
        public string ModelFile { get; set; }
        [YamlMember(Alias = "pathFromRoot")]
        [JsonProperty("pathFromRoot")]
        public string LocalPathFromRepoRoot { get; set; }
        [YamlMember(Alias = "original")]
        [JsonProperty("original")]
        public string OriginalFile { get; set; }
        [YamlMember(Alias = "resource")]
        [JsonProperty("resource")]
        public string ResourceFile { get; set; }
        [JsonIgnore]
        [YamlIgnore]
        public FileModel Model { get; set; }
    }
}
