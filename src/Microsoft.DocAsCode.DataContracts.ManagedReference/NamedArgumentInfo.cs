// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class NamedArgumentInfo
    {
        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public string Type { get; set; }
        [YamlMember(Alias = "value")]
        [JsonProperty("value")]
        public object Value { get; set; }
    }
}
