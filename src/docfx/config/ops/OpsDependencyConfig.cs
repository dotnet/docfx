// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
internal class OpsDependencyConfig
{
    public PathString PathToRoot { get; init; }

    public string Url { get; init; } = "";

    public string Branch { get; init; } = "main";

    public Dictionary<string, string> BranchMapping { get; init; } = new Dictionary<string, string>();

    public bool IncludeInBuild { get; init; }
}
