// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal class OpsDependencyConfig
    {
        public readonly string PathToRoot = "";

        public readonly string Url = "";

        public readonly string Branch = "master";

        public readonly Dictionary<string, string> BranchMapping = new Dictionary<string, string>();

        public readonly bool IncludeInBuild;
    }
}
