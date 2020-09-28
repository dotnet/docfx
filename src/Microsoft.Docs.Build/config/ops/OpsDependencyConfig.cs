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
        public PathString PathToRoot { get; private set; }

        public string Url { get; private set; } = "";

        public string Branch { get; private set; } = "main";

        public Dictionary<string, string> BranchMapping { get; private set; } = new Dictionary<string, string>();

        public bool IncludeInBuild { get; private set; }
    }
}
