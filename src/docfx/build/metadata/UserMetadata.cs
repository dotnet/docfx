// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class UserMetadata
    {
        [JsonProperty("titleSuffix")]
        public string? TitleSuffix { get; private set; }

        public SourceInfo<string?> Title { get; private set; }

        public string? Layout { get; private set; }

        public string? PageType { get; private set; }

        public SourceInfo<string> Author { get; private set; } = new SourceInfo<string>("");

        public SourceInfo<string> BreadcrumbPath { get; private set; } = new SourceInfo<string>("");

        [JsonProperty("monikerRange")]
        public SourceInfo<string?> MonikerRange { get; private set; }

        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[]? Monikers { get; private set; }

        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[]? ExcludeMonikers { get; private set; }

        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string>[]? ReplaceMonikers { get; private set; }

        public SourceInfo<string> Uid { get; private set; } = new SourceInfo<string>("");

        [JsonProperty("_tocRel")]
        public string? TocRel { get; private set; }

        public string? Robots { get; set; }

        public PathString TildePath { get; private set; }

        [JsonIgnore]
        public JObject RawJObject { get; set; } = new JObject();
    }
}
