// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.RestApi
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common.EntityMergers;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class RestApiItemViewModelBase : IOverwriteDocumentViewModel
    {
        [YamlMember(Alias = Constants.PropertyName.Uid)]
        [JsonProperty(Constants.PropertyName.Uid)]
        [MergeOption(MergeOption.MergeKey)]
        public string Uid { get; set; }

        [YamlMember(Alias = "htmlId")]
        [JsonProperty("htmlId")]
        [MergeOption(MergeOption.Ignore)]
        public string HtmlId { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Conceptual)]
        [JsonProperty(Constants.PropertyName.Conceptual)]
        public string Conceptual { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "remarks")]
        [JsonProperty("remarks")]
        public string Remarks { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Documentation)]
        [JsonProperty(Constants.PropertyName.Documentation)]
        [MergeOption(MergeOption.Ignore)]
        public SourceDetail Documentation { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
