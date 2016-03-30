// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility.Git
{
    using System;

    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public sealed class UserInfo
    {
        [YamlMember(Alias = "name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [YamlMember(Alias = "email")]
        [JsonProperty("email")]
        public string Email { get; set; }

        [YamlMember(Alias = "date")]
        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
}
