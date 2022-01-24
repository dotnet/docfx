// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class UserMetadata
{
    [JsonProperty("titleSuffix")]
    public string? TitleSuffix { get; init; }

    public SourceInfo<string?> Title { get; init; }

    public string? Layout { get; init; }

    public string? PageType { get; init; }

    public SourceInfo<string> Author { get; init; } = new SourceInfo<string>("");

    [JsonProperty("ms.author")]
    public string? MsAuthor { get; init; }

    [JsonProperty("ms.prod")]
    public string? MsProd { get; init; }

    [JsonProperty("ms.technology")]
    public string? MsTechnology { get; init; }

    [JsonProperty("ms.service")]
    public string? MsService { get; init; }

    [JsonProperty("ms.subservice")]
    public string? MsSubservice { get; init; }

    [JsonProperty("ms.topic")]
    public string? MsTopic { get; init; }

    public SourceInfo<string> BreadcrumbPath { get; init; } = new SourceInfo<string>("");

    [JsonProperty("monikerRange")]
    public SourceInfo<string?> MonikerRange { get; init; }

    [JsonConverter(typeof(OneOrManyConverter))]
    public SourceInfo<string>[]? Monikers { get; init; }

    [JsonConverter(typeof(OneOrManyConverter))]
    public SourceInfo<string>[]? ExcludeMonikers { get; init; }

    [JsonConverter(typeof(OneOrManyConverter))]
    public SourceInfo<string>[]? ReplaceMonikers { get; init; }

    public SourceInfo<string> Uid { get; init; } = new SourceInfo<string>("");

    [JsonProperty("_tocRel")]
    public string? TocRel { get; init; }

    public string? Robots { get; set; }

    public PathString TildePath { get; init; }

    public bool IsArchived { get; init; }

    public string? ContentGitUrl { get; init; }

    public string? OriginalContentGitUrl { get; init; }

    public string? OriginalContentGitUrlTemplate { get; init; }

    /// <summary>
    /// Published zone pivot groups definition filename (not the source file, should ends with .json)
    /// </summary>
    public string? ZonePivotGroupFilename { get; init; }

    public string? ZonePivotGroups { get; init; }

    [JsonIgnore]
    public JObject RawJObject { get; set; } = new JObject();

    public bool NoIndex()
    {
        return Robots != null && Robots.Contains("noindex", StringComparison.OrdinalIgnoreCase);
    }
}
