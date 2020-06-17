// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class UserMetadata
    {
        public string? Title { get; private set; }

        public string? Layout { get; private set; }

        public SourceInfo<string> Author { get; private set; } = new SourceInfo<string>("");

        public SourceInfo<string> BreadcrumbPath { get; private set; } = new SourceInfo<string>("");

        [JsonProperty("monikerRange")]
        public SourceInfo<string?> MonikerRange { get; private set; }

        [JsonConverter(typeof(OneOrManyConverter))]
        public SourceInfo<string?>[]? Monikers { get; private set; }

        public SourceInfo<string> Uid { get; private set; } = new SourceInfo<string>("");

        [JsonProperty("_tocRel")]
        public string? TocRel { get; private set; }

        public string? Robots { get; set; }

        [JsonIgnore]
        public JObject RawJObject { get; set; } = new JObject();
    }
}
