// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ApiLanguageValuePairWithLevel<T> : ApiLanguageValuePair<T>
    {
        [YamlMember(Alias = "level")]
        [JsonProperty("level")]
        public int Level { get; set; }
    }
}