// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using Microsoft.DocAsCode.DataContracts.Attributes;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public class ArgumentInfo
    {
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        [UniqueIdentityReference]
        public string Type { get; set; }

        [YamlMember(Alias = "value")]
        [JsonProperty("value")]
        public object Value { get; set; }
    }
}
