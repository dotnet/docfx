// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Newtonsoft.Json;

    public class MetadataJsonItemConfig
    {
        [JsonProperty("src")]
        public FileMapping Source { get; set; }

        [JsonProperty("dest")]
        public string Destination { get; set; }

        [JsonProperty("force")]
        public bool? Force { get; set; }

        [JsonProperty("skip")]
        public bool? IsSkipMarkup { get; set; }

        [JsonProperty("raw")]
        public bool? Raw { get; set; }

        [JsonProperty("filter")]
        public string FilterConfigFile { get; set; }

        [JsonProperty("useCompatibilityFileName")]
        public bool? UseCompatibilityFileName { get; set; }
    }

}
