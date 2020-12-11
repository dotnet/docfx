// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal record PublishItem(
        string Url,
        string? Path,
        [property: JsonIgnore] FilePath? SourceFile,
        string? SourcePath, // File source relative path to docset root will be used for PR comments
        string Locale,
        [property: JsonIgnore] MonikerList Monikers,
        string? ConfigMonikerRange,
        [property: JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] bool HasError,
        [property: JsonExtensionData] JObject? ExtensionData)
    {
        public string? MonikerGroup => Monikers.MonikerGroup;
    }
}
