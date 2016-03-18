// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Utility.EntityMergers;
    using Newtonsoft.Json;

    [Serializable]
    public class CrefInfo
    {
        [YamlMember(Alias = "type")]
        [MergeOption(MergeOption.MergeKey)]
        [JsonProperty("type")]
        public string Type { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
