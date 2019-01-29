// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class TableOfContentsMetadata
    {
        public List<string> Monikers { get; set; }

        [JsonProperty(PropertyName = "monikerRange")]
        public string MonikerRange { get; set; }

        public string PdfAbsolutePath { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Experimental { get; set; }

        public string ExperimentId { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }

        public bool ShouldSerializeMonikerRange() => false;
    }
}
