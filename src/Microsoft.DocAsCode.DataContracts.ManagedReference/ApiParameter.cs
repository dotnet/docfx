// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common.EntityMergers;
    using Microsoft.DocAsCode.DataContracts.Common;

    [Serializable]
    public class ApiParameter
    {
        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        [MergeOption(MergeOption.MergeKey)]
        public string Name { get; set; }

        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        [UniqueIdentityReference]
        public string Type { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        [MarkdownContent]
        public string Description { get; set; }

        [YamlMember(Alias = "attributes")]
        [JsonProperty("attributes")]
        [MergeOption(MergeOption.Ignore)]
        public List<AttributeInfo> Attributes { get; set; }

        public void CopyInheritedData(ApiParameter src)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            if (Description == null)
                Description = src.Description;
        }
    }
}
