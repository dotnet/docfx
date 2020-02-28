// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

#nullable enable

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class TableOfContentsMetadata
    {
        public IReadOnlyList<string> Monikers { get; set; } = Array.Empty<string>();

        [JsonProperty(PropertyName = "monikerRange")]
        public SourceInfo<string?> MonikerRange { get; set; }

        public string? PdfAbsolutePath { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; } = new JObject();

        public bool ShouldSerializeMonikerRange() => false;
    }
}
