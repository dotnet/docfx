// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;
    using Microsoft.DocAsCode.DataContracts.Common;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class NamedArgumentInfo
    {
        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        [UniqueIdentityReference]
        public string Type { get; set; }

        [YamlMember(Alias = "value")]
        [JsonProperty("value")]
        public object Value { get; set; }
    }
}
