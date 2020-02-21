// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#nullable enable

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class OpsDependencyConfig
    {
        public string PathToRoot { get; } = "";

        public string Url { get; } = "";

        public string Branch { get; } = "master";

        public Dictionary<string, string> BranchMapping { get; } = new Dictionary<string, string>();

        public bool IncludeInBuild { get; }
    }
}
