// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ECMA2Yaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class OpsConfig
{
    public OpsDocsetConfig[] DocsetsToPublish { get; init; } = Array.Empty<OpsDocsetConfig>();

    public OpsDependencyConfig[] DependentRepositories { get; init; } = Array.Empty<OpsDependencyConfig>();

    public string? GitRepositoryBranchOpenToPublicContributors { get; init; }

    public string? GitRepositoryUrlOpenToPublicContributors { get; init; }

    public bool NeedGeneratePdfUrlTemplate { get; init; }

    public string? XrefEndpoint { get; init; }

    [JsonProperty(nameof(JoinTOCPlugin))]
    public OpsJoinTocConfig[]? JoinTOCPlugin { get; init; }

    [JsonProperty(nameof(SplitTOC))]
    [JsonConverter(typeof(OneOrManyConverter))]
    public PathString[] SplitTOC { get; init; } = Array.Empty<PathString>();

    [JsonProperty(nameof(ECMA2Yaml))]
    [JsonConverter(typeof(OneOrManyConverter))]
    public ECMA2YamlRepoConfig[]? ECMA2Yaml { get; init; }

    [JsonProperty("monikerPath")]
    [JsonConverter(typeof(OneOrManyConverter))]
    public string[]? MonikerPath { get; init; }
}
