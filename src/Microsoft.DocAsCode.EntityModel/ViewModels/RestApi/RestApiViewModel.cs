// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Utility.EntityMergers;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class RestApiViewModel
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        [MergeOption(MergeOption.MergeKey)]
        public string Uid { get; set; }

        [YamlMember(Alias = "htmlId")]
        [JsonProperty("htmlId")]
        public string HtmlId { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "children")]
        [JsonProperty("children")]
        public List<RestApiItemViewModel> Children { get; set; } = new List<RestApiItemViewModel>();

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
