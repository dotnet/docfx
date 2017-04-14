// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.RestApi
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    using Microsoft.DocAsCode.DataContracts.Common;

    [Serializable]
    public class RestApiChildItemViewModel : RestApiItemViewModelBase
    {
        [YamlMember(Alias = Constants.PropertyName.Path)]
        [JsonProperty(Constants.PropertyName.Path)]
        public string Path { get; set; }

        /// <summary>
        /// operation name, for example get, put, post, delete, options, head and patch.
        /// </summary>
        [YamlMember(Alias = "operation")]
        [JsonProperty("operation")]
        public string OperationName { get; set; }

        [YamlMember(Alias = "operationId")]
        [JsonProperty("operationId")]
        public string OperationId { get; set; }

        [YamlMember(Alias = "tags")]
        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<RestApiParameterViewModel> Parameters { get; set; }

        [YamlMember(Alias = "responses")]
        [JsonProperty("responses")]
        public List<RestApiResponseViewModel> Responses { get; set; }
    }
}
