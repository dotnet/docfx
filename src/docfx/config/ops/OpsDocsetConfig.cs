// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ECMA2Yaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class OpsDocsetConfig
{
    public string DocsetName { get; init; } = "";

    public PathString BuildSourceFolder { get; init; }

    public bool OpenToPublicContributors { get; init; }

    public string[] XrefQueryTags { get; init; } = Array.Empty<string>();

    public Dictionary<string, string[]>? CustomizedTasks { get; init; }

    [JsonProperty(nameof(SplitTOC))]
    public HashSet<PathString> SplitTOC { get; init; } = new HashSet<PathString>();

    [JsonProperty(nameof(JoinTOCPlugin))]
    public OpsJoinTocConfig[]? JoinTOCPlugin { get; init; }

    [JsonProperty(nameof(ECMA2Yaml))]
    [JsonConverter(typeof(OneOrManyConverter))]
    public ECMA2YamlRepoConfig[]? ECMA2Yaml { get; init; }

    [JsonProperty(nameof(MonikerPath))]
    [JsonConverter(typeof(OneOrManyConverter))]
    public string[]? MonikerPath { get; init; }
}
