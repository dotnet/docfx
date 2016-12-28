// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.JavaScriptReference
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ApiSyntaxBuildOutput
    {
        [YamlMember(Alias = "content")]
        [JsonProperty("content")]
        public List<ApiLanguageValuePair> Content { get; set; } = new List<ApiLanguageValuePair>();

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<ApiParameterBuildOutput> Parameters { get; set; }

        [YamlMember(Alias = "return")]
        [JsonProperty("return")]
        public ApiParameterBuildOutput Return { get; set; }
    }
}
