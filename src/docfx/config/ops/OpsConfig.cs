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
        public readonly OpsDocsetConfig[] DocsetsToPublish = Array.Empty<OpsDocsetConfig>();

        public readonly OpsDependencyConfig[] DependentRepositories = Array.Empty<OpsDependencyConfig>();

        public readonly string? GitRepositoryBranchOpenToPublicContributors;

        public readonly string? GitRepositoryUrlOpenToPublicContributors;

        public readonly bool NeedGeneratePdfUrlTemplate;

        public readonly string? XrefEndpoint;
    }
}
