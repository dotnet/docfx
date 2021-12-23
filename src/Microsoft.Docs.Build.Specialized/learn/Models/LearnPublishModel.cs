// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.LearnValidation.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class LearnPublishModel
{
    public List<LearnPublishItem> Files { get; } = new List<LearnPublishItem>();

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
