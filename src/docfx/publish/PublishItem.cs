// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public string Locale { get; set; }

        public string RedirectUrl { get; set; }

        [JsonIgnore]
        public List<string> Monikers { get; set; } = new List<string>();

        [JsonExtensionData]
        public JObject ExtensionData { get; set; }
    }
}
