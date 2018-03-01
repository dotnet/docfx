// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.UniversalReference
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common.EntityMergers;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ExceptionInfo
    {
        [YamlMember(Alias = "type")]
        [MergeOption(MergeOption.MergeKey)]
        [JsonProperty("type")]
        [UniqueIdentityReference]
        public string Type { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        [MarkdownContent]
        public string Description { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
