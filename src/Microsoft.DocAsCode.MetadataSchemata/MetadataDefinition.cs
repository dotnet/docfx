// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MetadataSchemata
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal sealed class MetadataDefinition : IMetadataDefinition
    {
        [JsonProperty("type", Required = Required.Always)]
        public string Type { get; set; }
        [JsonProperty("is_multivalued", Required = Required.Always)]
        public bool IsMultiValued { get; set; }
        [JsonProperty("is_queryable", Required = Required.Always)]
        public bool IsQueryable { get; set; }
        [JsonProperty("is_required", Required = Required.Always)]
        public bool IsRequired { get; set; }
        [JsonProperty("is_visible", Required = Required.Always)]
        public bool IsVisible { get; set; }
        [JsonProperty("query_name", Required = Required.Default)]
        public string QueryName { get; set; }
        [JsonProperty("display_name", Required = Required.Always)]
        public string DisplayName { get; set; }
        [JsonProperty("choice_set", Required = Required.Default)]
        public List<JValue> ChoiceSet { get; set; }
        [JsonProperty("description", Required = Required.Default)]
        public string Description { get; set; }
    }
}
