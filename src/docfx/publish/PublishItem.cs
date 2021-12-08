// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class PublishItem
{
    public string? Url { get; init; }

    public string? Path { get; init; }

    [JsonIgnore]
    public FilePath? SourceFile { get; init; }

    /// <summary>
    /// File source relative path to docset root will be used for PR comment
    /// </summary>
    public string? SourcePath { get; init; }

    public string? Locale { get; init; }

    [JsonIgnore]
    public MonikerList Monikers { get; init; }

    public string? ConfigMonikerRange { get; init; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool HasError { get; init; }

    [JsonExtensionData]
    public JObject? ExtensionData { get; init; }

    public string? MonikerGroup => Monikers.MonikerGroup;
}
