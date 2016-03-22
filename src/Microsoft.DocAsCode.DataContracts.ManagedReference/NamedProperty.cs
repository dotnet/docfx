// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class NamedProperty
    {
        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Name { get; set; }
        [YamlMember(Alias = "property")]
        [JsonProperty("property")]
        public string Property { get; set; }
        [YamlMember(Alias = "declareType")]
        [JsonProperty("declareType")]
        public string DeclareType { get; set; }
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public string Type { get; set; }
        [YamlMember(Alias = "value")]
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
