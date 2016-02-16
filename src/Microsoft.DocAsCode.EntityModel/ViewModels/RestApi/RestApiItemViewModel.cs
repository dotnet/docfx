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
    public class RestApiItemViewModel
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        [MergeOption(MergeOption.MergeKey)]
        public string Uid { get; set; }

        [YamlMember(Alias = "htmlId")]
        [JsonProperty("htmlId")]
        public string HtmlId { get; set; }

        [YamlMember(Alias = "path")]
        [JsonProperty("path")]
        public string Path { get; set; }

        [YamlMember(Alias = "operation")]
        [JsonProperty("operation")]
        public string OperationName { get; set; }

        [YamlMember(Alias = "operationId")]
        [JsonProperty("operationId")]
        public string OperationId { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<RestApiParameterViewModel> Parameters { get; set; }

        [YamlMember(Alias = "responses")]
        [JsonProperty("responses")]
        public List<RestApiResponseViewModel> Responses { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
