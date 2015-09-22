// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    class BuildJsonConfig
    {
        [JsonProperty("content")]
        public FileMapping Content { get; set; }

        [JsonProperty("resource")]
        public FileMapping Resource { get; set; }

        [JsonProperty("overwrite")]
        public FileMapping Overwrite { get; set; }

        [JsonProperty("externalReference")]
        public FileMapping ExternalReference { get; set; }

        [JsonProperty("dest")]
        public string Destination { get; set; }

        [JsonProperty("globalMetadata")]
        public Dictionary<string, object> GlobalMetadata { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("template")]
        public string Template { get; set; }

        [JsonProperty("templateFolder")]
        public string TemplateFolder { get; set; }

        [JsonProperty("theme")]
        public string TemplateTheme { get; set; }

        [JsonProperty("themeFolder")]
        public string TemplateThemeFolder { get; set; }
    }

}
