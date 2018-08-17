// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    [DataSchema]
    public class ContextObject
    {
        [JsonProperty("brand")]
        public string Brand { get; set; }

        [Href]
        [JsonProperty("breadcrumb_path")]
        public string BreadcrumbPath { get; set; }

        [JsonProperty("chromeless")]
        public bool Chromeless { get; set; }

        [JsonProperty("searchScope")]
        public string[] SearchScope { get; set; }

        [Href]
        [JsonProperty("toc_rel")]
        public string TocRel { get; set; }

        [JsonProperty("uhfheaderId")]
        public string UhfHeaderId { get; set; }
    }
}
