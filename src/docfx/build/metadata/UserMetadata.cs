// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class UserMetadata
    {
        public string Title { get; set; }

        public SourceInfo<string> Author { get; set; }

        public SourceInfo<string> BreadcrumbPath { get; set; }

        [JsonProperty("monikerRange")]
        public SourceInfo<string> MonikerRange { get; set; }

        public string Uid { get; set; }

        [JsonProperty("_tocRel")]
        public string TocRel { get; set; }

        [JsonIgnore]
        public JObject RawJObject { get; set; } = new JObject();
    }
}
