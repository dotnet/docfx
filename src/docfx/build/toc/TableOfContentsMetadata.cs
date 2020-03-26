// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class TableOfContentsMetadata
    {
        [JsonProperty(PropertyName = "monikerRange")]
        public SourceInfo<string?> MonikerRange { get; set; }

        public string? PdfAbsolutePath { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();

        public bool ShouldSerializeMonikerRange() => false;
    }
}
