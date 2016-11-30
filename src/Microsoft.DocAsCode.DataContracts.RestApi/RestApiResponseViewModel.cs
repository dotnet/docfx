// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.RestApi
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common.EntityMergers;
    using Microsoft.DocAsCode.YamlSerialization;

    [Serializable]
    public class RestApiResponseViewModel
    {
        [YamlMember(Alias = "statusCode")]
        [JsonProperty("statusCode")]
        [MergeOption(MergeOption.MergeKey)]
        public string HttpStatusCode { get; set; }

        [YamlMember(Alias = "description")]
        [JsonProperty("description")]
        public string Description { get; set; }

        [YamlMember(Alias = "summary")]
        [JsonProperty("summary")]
        public string Summary { get; set; }

        [YamlMember(Alias = "examples")]
        [JsonProperty("examples")]
        public List<RestApiResponseExampleViewModel> Examples { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
