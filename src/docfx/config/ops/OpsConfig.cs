// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#nullable enable

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class OpsConfig
    {
        public OpsDocsetConfig[] DocsetsToPublish { get; } = Array.Empty<OpsDocsetConfig>();

        public OpsDependencyConfig[] DependentRepositories { get; } = Array.Empty<OpsDependencyConfig>();

        public string? GitRepositoryBranchOpenToPublicContributors { get; }

        public string? GitRepositoryUrlOpenToPublicContributors { get; }

        public bool NeedGeneratePdfUrlTemplate { get; }

        public string? XrefEndpoint { get; }
    }
}
