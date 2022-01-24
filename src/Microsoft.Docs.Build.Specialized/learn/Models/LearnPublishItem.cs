// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.LearnValidation.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class LearnPublishItem
{
    public string SourcePath { get; set; } = "";

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool HasError { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
