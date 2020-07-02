// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ECMA2Yaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class OpsConfig
    {
        // TODO: remove this switch
        public bool DisableRemoveHost { get; private set; }

        public OpsDocsetConfig[] DocsetsToPublish { get; private set; } = Array.Empty<OpsDocsetConfig>();

        public OpsDependencyConfig[] DependentRepositories { get; private set; } = Array.Empty<OpsDependencyConfig>();

        public string? GitRepositoryBranchOpenToPublicContributors { get; private set; }

        public string? GitRepositoryUrlOpenToPublicContributors { get; private set; }

        public bool NeedGeneratePdfUrlTemplate { get; private set; }

        public string? XrefEndpoint { get; private set; }

        [JsonProperty(nameof(JoinTOCPlugin))]
        public OpsJoinTocConfig[]? JoinTOCPlugin { get; private set; }

        [JsonProperty(nameof(ECMA2Yaml))]
        [JsonConverter(typeof(OneOrManyConverter))]
        public ECMA2YamlRepoConfig[]? ECMA2Yaml { get; private set; }
    }
}
