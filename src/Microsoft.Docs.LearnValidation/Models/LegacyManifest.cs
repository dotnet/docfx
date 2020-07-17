// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TripleCrownValidation
{
    public class LegacyManifest
    {
        public LegacyManifestItem[] Files { get; set; } = Array.Empty<LegacyManifestItem>();
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class LegacyManifestItem
    {
        public string? AssetId { get; set; }

        public string? SourceRelativePath { get; set; }

        public string OriginalType { get; set; } = string.Empty;

        public LegacyManifestOutput? Output { get; set; }
    }


    public class LegacyManifestOutput
    {
        [JsonProperty(".mta.json", NullValueHandling = NullValueHandling.Ignore)]
        public LegacyManifestOutputItem? MetadataOutput { get; set; }
    }

    public class LegacyManifestOutputItem
    {
        // output path relative to site base path
        [JsonProperty("relative_path")]
        public string? RelativePath { get; set; }

        /// <summary>
        /// Gets or sets output absolute path, used when output not within build output directory
        /// e.g. resource's output when <see cref="OutputConfig.CopyResources"/> = false
        /// </summary>
        [JsonProperty("link_to_path")]
        public string? LinkToPath { get; set; }
    }
}
