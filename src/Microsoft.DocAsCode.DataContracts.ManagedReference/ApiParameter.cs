// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;

    using Microsoft.DocAsCode.Utility.EntityMergers;
    using Newtonsoft.Json;

    [Serializable]
    public class ApiParameter
    {
        [YamlDotNet.Serialization.YamlMember(Alias = "id")]
        [JsonProperty("id")]
        [MergeOption(MergeOption.MergeKey)]
        public string Name { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "type")]
        [JsonProperty("type")]
        public string Type { get; set; }

        [YamlDotNet.Serialization.YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
