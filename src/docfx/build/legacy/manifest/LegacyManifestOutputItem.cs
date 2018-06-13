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
        public string OutputPathRelativeToSiteBasePath { get; set; }
    }
}
