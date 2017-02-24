// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class Manifest
    {
        public Manifest() { }

        public Manifest(IEnumerable<ManifestItem> files)
        {
            Files.AddRange(files);
        }

        [JsonProperty("templates")]
        public List<string> Templates { get; set; }

        [JsonProperty("homepages")]
        public List<HomepageInfo> Homepages { get; set; }

        [JsonProperty("source_base_path")]
        public string SourceBasePath { get; set; }

        [JsonProperty("xrefmap")]
        public object XRefMap { get; set; }

        [JsonProperty("files")]
        public ManifestItemCollection Files { get; } = new ManifestItemCollection();

        [JsonProperty("incremental_info")]
        public List<IncrementalInfo> IncrementalInfo { get; set; }
    }
}
