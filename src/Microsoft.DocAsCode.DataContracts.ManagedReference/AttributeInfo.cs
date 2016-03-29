// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class AttributeInfo
    {
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public string Type { get; set; }
        [YamlMember(Alias = "ctor")]
        [JsonProperty("ctor")]
        public string Constructor { get; set; }
        [YamlMember(Alias = "arguments")]
        [JsonProperty("arguments")]
        public List<ArgumentInfo> Arguments { get; set; }
        [YamlMember(Alias = "namedArguments")]
        [JsonProperty("namedArguments")]
        public List<NamedArgumentInfo> NamedArguments { get; set; }
    }
}
