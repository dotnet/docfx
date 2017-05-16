// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class PdfJsonConfig : BuildJsonConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("host")]
        public new string Host { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("generatesAppendices")]
        public bool GeneratesAppendices { get; set; }

        [JsonProperty("generatesExternalLink")]
        public bool GeneratesExternalLink { get; set; }

        [JsonProperty("excludedTocs")]
        public List<string> ExcludedTocs { get; set; }

        [JsonProperty("css")]
        public string CssFilePath { get; set; }

        [JsonProperty("base")]
        public string BasePath { get; set; }
    }
}
