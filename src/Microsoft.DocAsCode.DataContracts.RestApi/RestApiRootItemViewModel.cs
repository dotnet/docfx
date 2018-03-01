// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.RestApi
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.Common.EntityMergers;

    [Serializable]
    public class RestApiRootItemViewModel : RestApiItemViewModelBase
    {
        /// <summary>
        /// The original swagger.json content
        /// `_` prefix indicates that this metadata is generated
        /// </summary>
        [YamlMember(Alias = "_raw")]
        [JsonProperty("_raw")]
        [MergeOption(MergeOption.Ignore)]
        public string Raw { get; set; }

        [YamlMember(Alias = "tags")]
        [JsonProperty("tags")]
        public List<RestApiTagViewModel> Tags { get; set; }

        [YamlMember(Alias = "children")]
        [JsonProperty("children")]
        public List<RestApiChildItemViewModel> Children { get; set; }
    }
}
