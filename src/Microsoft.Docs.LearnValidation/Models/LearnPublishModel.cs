// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.LearnValidation.Models
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class LearnPublishModel
    {
        public List<LearnPublishItem> Files { get; } = new List<LearnPublishItem>();

        [JsonExtensionData]
        public JObject ExtensionData { get; private set; }
    }

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class LearnPublishItem
    {
        public string SourcePath { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HasError { get; set; }

        [JsonExtensionData]
        public JObject ExtensionData { get; private set; }
    }
}
