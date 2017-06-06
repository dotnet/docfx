// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.ManagedReference.BuildOutputs
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public class ApiNames
    {
        [YamlMember(Alias = "uid")]
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string Id { get; set; }

        [YamlMember(Alias = "definition")]
        [JsonProperty("definition")]
        public string Definition { get; set; }

        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public List<ApiLanguageValuePair> Name { get; set; }

        [YamlMember(Alias = "nameWithType")]
        [JsonProperty("nameWithType")]
        public List<ApiLanguageValuePair> NameWithType { get; set; }

        [YamlMember(Alias = "fullName")]
        [JsonProperty("fullName")]
        public List<ApiLanguageValuePair> FullName { get; set; }

        [YamlMember(Alias = "specName")]
        [JsonProperty("specName")]
        public List<ApiLanguageValuePair> Spec { get; set; }

        public static ApiNames FromUid(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return null;
            }
            return new ApiNames
            {
                Uid = uid,
            };
        }
    }
}