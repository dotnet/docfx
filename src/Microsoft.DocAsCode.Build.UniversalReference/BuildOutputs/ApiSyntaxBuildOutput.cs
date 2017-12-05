// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.YamlSerialization;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ApiSyntaxBuildOutput
    {
        [YamlMember(Alias = Constants.PropertyName.Content)]
        [JsonProperty(Constants.PropertyName.Content)]
        public List<ApiLanguageValuePair<string>> Content { get; set; }

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<ApiParameterBuildOutput> Parameters { get; set; }

        [YamlMember(Alias = "typeParameters")]
        [JsonProperty("typeParameters")]
        public List<ApiParameterBuildOutput> TypeParameters { get; set; }

        [YamlMember(Alias = Constants.PropertyName.Return)]
        [JsonProperty(Constants.PropertyName.Return)]
        public List<ApiLanguageValuePair<ApiParameterBuildOutput>> Return { get; set; }

        [ExtensibleMember]
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
