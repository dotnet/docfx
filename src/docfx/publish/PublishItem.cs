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

        /// <summary>
        /// File source relative path to docset root
        /// will be used for PR comments
        /// </summary>
        public string? SourcePath { get; }

        [JsonIgnore]
        public MonikerList Monikers { get; }

        public string? MonikerGroup => Monikers.MonikerGroup;

        public string? ConfigMonikerRange { get; }

        public string Locale { get; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HasError { get; }

        [JsonExtensionData]
        public JObject? ExtensionData { get; }

        [JsonIgnore]
        public ContentType ContentType { get; }

        [JsonIgnore]
        public string? Mime { get; }

        public PublishItem(
            string url,
            string? path,
            string? sourcePath,
            string locale,
            MonikerList monikers,
            string? configMonikerRange,
            ContentType contentType,
            string? mime,
            bool hasError,
            JObject? extensionData)
        {
            Url = url;
            Path = path;
            SourcePath = sourcePath;
            Locale = locale;
            Monikers = monikers;
            ConfigMonikerRange = configMonikerRange;
            ContentType = contentType;
            Mime = mime;
            HasError = hasError;
            ExtensionData = extensionData;
        }
    }
}
