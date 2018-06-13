// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class LegacyManifestItem
    {
        // published url relative to site base path
        [JsonProperty("asset_id")]
        public string SiteUrlRelativeToSiteBasePath { get; set; }

        // rource path relative to source repo root
        [JsonProperty("original")]
        public string FilePath { get; set; }

        // source path relative to source base path
        [JsonProperty("source_relative_path")]
        public string FilePathRelativeToSourceBasePath { get; set; }

        [JsonProperty("original_type")]
        public string OriginalType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("output")]
        public LegacyManifestOutput Output { get; set; }

        // tell ops to use plugin for normalization
        [JsonProperty("skip_normalization")]
        public bool SkipNormalization { get; set; }
    }
}
