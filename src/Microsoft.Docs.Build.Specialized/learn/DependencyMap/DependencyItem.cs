// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.LearnValidation;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class DependencyItem
{
    public string FromFilePath { get; set; } = "";

    public string ToFilePath { get; set; } = "";

    public string DependencyType { get; set; } = "";

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? Version { get; set; }
}
