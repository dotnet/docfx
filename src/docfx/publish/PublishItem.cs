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
    internal class PublishItem
    {
        public string Url { get; set; }

        public string Path { get; set; }

        public string SourcePath { get; set; }

        public string MonikerGroup { get; set; }

        [JsonIgnore]
        public string ConfigMonikerRange { get; set; }

        public string Locale { get; set; }

        public string RedirectUrl { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HasError { get; set; }

        [JsonIgnore]
        public IReadOnlyList<string> Monikers { get; set; } = Array.Empty<string>();

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }
    }
}
