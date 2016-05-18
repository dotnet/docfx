// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ApiTypeAndSpec
    {

        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "specName")]
        [JsonProperty("specName")]
        public List<ApiLanguageValuePair> Spec { get; set; }
    }
}