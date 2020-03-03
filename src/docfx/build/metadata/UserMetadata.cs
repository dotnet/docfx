// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

#nullable enable

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class UserMetadata
    {
        public string? Title { get; private set; }

        public SourceInfo<string?> Author { get; private set; }

        public SourceInfo<string?> BreadcrumbPath { get; private set; }

        [JsonProperty("monikerRange")]
        public SourceInfo<string> MonikerRange { get; private set; } = new SourceInfo<string>("");

        public string? Uid { get; private set; }

        [JsonProperty("_tocRel")]
        public string? TocRel { get; private set; }

        [JsonIgnore]
        public JObject RawJObject { get; set; } = new JObject();
    }
}
