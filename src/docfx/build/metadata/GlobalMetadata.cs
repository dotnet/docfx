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
        [Obsolete("v2 backward compatibility")]
        public string[] ContributorsToExclude { get; set; } = Array.Empty<string>();

        // For v2 backward compatibility
        [JsonProperty("_op_documentIdPathDepotMapping")]
        public Dictionary<PathString, DocumentIdConfig> DocumentIdDepotMapping { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; set; } = new JObject();
    }
}
