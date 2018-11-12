// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class LegacyManifestOutputItem
    {
        [JsonProperty("is_raw_page")]
        public bool IsRawPage { get; set; }

        // output path relative to site base path
        [JsonProperty("relative_path")]
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or sets output absolute path, used when output not within build output directory
        /// e.g. resource's output when <see cref="OutputConfig.CopyResources"/> = false
        /// </summary>
        [JsonProperty("link_to_path")]
        public string LinkToPath { get; set; }
    }
}
