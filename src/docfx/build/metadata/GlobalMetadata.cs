// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class GlobalMetadata
    {
        // Backward compatibility with v2
        public string[] ContributorsToExclude { get; set; } = Array.Empty<string>();

        // Backward compatibility with v2
        [JsonProperty("_op_documentIdPathDepotMapping")]
        public Dictionary<string, DocumentIdDepotMapping> DocumentIdDepotMapping { get; set; }
         = new Dictionary<string, DocumentIdDepotMapping>(PathUtility.PathComparer);

        [JsonExtensionData]
        public JObject ExtensionData { get; set; } = new JObject();
    }
}
