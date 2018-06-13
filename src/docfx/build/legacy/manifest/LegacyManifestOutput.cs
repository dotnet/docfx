// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class LegacyManifestOutput
    {
        [JsonProperty(".mta.json", NullValueHandling = NullValueHandling.Ignore)]
        public LegacyManifestOutputItem MetadataOutput { get; set; }

        [JsonProperty(".raw.page.json", NullValueHandling = NullValueHandling.Ignore)]
        public LegacyManifestOutputItem PageOutput { get; set; }

        [JsonProperty(".json", NullValueHandling = NullValueHandling.Ignore)]
        public LegacyManifestOutputItem TocOutput { get; set; }

        [JsonProperty("resource", NullValueHandling = NullValueHandling.Ignore)]
        public LegacyManifestOutputItem ResourceOutput { get; set; }
    }
}
