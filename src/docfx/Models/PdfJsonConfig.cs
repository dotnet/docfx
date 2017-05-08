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
        public string Host { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("appendices")]
        public bool GenerateAppendices { get; set; }

        [JsonProperty("external")]
        public bool GeneratePdfExternalLink { get; set; }
    }
}
