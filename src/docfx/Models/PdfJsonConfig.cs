// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
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

        [JsonProperty("keepRawFiles")]
        public bool KeepRawFiles { get; set; }

        [JsonProperty("rawOutputFolder")]
        public string RawOutputFolder { get; set; }

        [JsonProperty("excludedTocs")]
        public List<string> ExcludedTocs { get; set; }

        [JsonProperty("css")]
        public string CssFilePath { get; set; }

        [JsonProperty("base")]
        public string BasePath { get; set; }

        /// <summary>
        /// Specify how to handle pages that fail to load: abort, ignore or skip(default abort)
        /// </summary>
        [JsonProperty("errorHandling")]
        public string LoadErrorHandling { get; set; }

        /// <summary>
        /// Specify options specific to the wkhtmltopdf tooling used by the pdf command.
        /// </summary>
        [JsonProperty("wkhtmltopdf")]
        public WkhtmltopdfJsonConfig Wkhtmltopdf { get; set; }
    }
}
