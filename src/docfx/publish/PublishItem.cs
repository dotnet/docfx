// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class PublishItem
    {
        public string Url { get; }

        public string? Path { get; }

        public string? SourcePath { get; }

        [JsonIgnore]
        public string[] Monikers { get; }

        public string? MonikerGroup { get; }

        public string? ConfigMonikerRange { get; }

        public string Locale { get; }

        public string? RedirectUrl { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HasError { get; set; }

        [JsonExtensionData]
        public JObject? ExtensionData { get; set; }

        public PublishItem(string url, string? path, string? sourcePath, string locale, string[] monikers, string? configMonikerRange)
        {
            Url = url;
            Path = path;
            SourcePath = sourcePath;
            Locale = locale;
            Monikers = monikers;
            ConfigMonikerRange = configMonikerRange;
            MonikerGroup = MonikerUtility.GetGroup(monikers);
        }
    }
}
