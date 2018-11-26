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
    public class ApiParameter
    {
        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        [MergeOption(MergeOption.MergeKey)]
        public string Name { get; set; }

        /// <summary>
        /// parameter's types
        /// multiple types is allowed for a parameter in languages like JavaScript
        /// </summary>
        [YamlMember(Alias = "type")]
        [JsonProperty("type")]
        [UniqueIdentityReference]
        public List<string> Type { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        [MarkdownContent]
        public string Description { get; set; }

        [YamlMember(Alias = "optional")]
        [JsonProperty("optional")]
        public bool Optional { get; set; }

        [YamlMember(Alias = "defaultValue")]
        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
