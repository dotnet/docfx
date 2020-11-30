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

        [JsonIgnore]
        public FilePath? SourceFile { get; }

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

        public PublishItem(
            string url,
            string? path,
            FilePath sourceFile,
            string? sourcePath,
            string locale,
            MonikerList monikers,
            string? configMonikerRange,
            bool hasError,
            JObject? extensionData)
        {
            Url = url;
            Path = path;
            SourceFile = sourceFile;
            SourcePath = sourcePath;
            Locale = locale;
            Monikers = monikers;
            ConfigMonikerRange = configMonikerRange;
            HasError = hasError;
            ExtensionData = extensionData;
        }
    }
}
