// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.ManagedReference
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class SyntaxDetailViewModel
    {
        [YamlMember(Alias = "content")]
        [JsonProperty("content")]
        public string Content { get; set; }

        [YamlMember(Alias = "content.csharp")]
        [JsonProperty("content.csharp")]
        public string ContentForCSharp { get; set; }

        [YamlMember(Alias = "content.vb")]
        [JsonProperty("content.vb")]
        public string ContentForVB { get; set; }

        [YamlMember(Alias = "parameters")]
        [JsonProperty("parameters")]
        public List<ApiParameter> Parameters { get; set; }

        [YamlMember(Alias = "typeParameters")]
        [JsonProperty("typeParameters")]
        public List<ApiParameter> TypeParameters { get; set; }

        [YamlMember(Alias = "return")]
        [JsonProperty("return")]
        public ApiParameter Return { get; set; }
    }
}
