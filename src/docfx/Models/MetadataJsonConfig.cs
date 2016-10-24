// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class MetadataJsonConfig : List<MetadataJsonItemConfig>
    {
        [JsonIgnore]
        public string BaseDirectory { get; set; }

        [JsonIgnore]
        public string OutputFolder { get; set; }

        [JsonIgnore]
        public bool Force { get; set; }

        [JsonIgnore]
        public bool ShouldSkipMarkup { get; set; }

        [JsonIgnore]
        public bool Raw { get; set; }

        public MetadataJsonConfig(IEnumerable<MetadataJsonItemConfig> configs) : base(configs) { }

        public MetadataJsonConfig(params MetadataJsonItemConfig[] configs) : base(configs)
        {
        }
    }
}
