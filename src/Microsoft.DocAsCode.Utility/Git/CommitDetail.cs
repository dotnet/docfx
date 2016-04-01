// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility.Git
{
    using System;
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    [Serializable]
    public sealed class CommitDetail
    {
        [YamlMember(Alias = "committer")]
        [JsonProperty("committer")]
        public UserInfo Committer { get; set; }

        [YamlMember(Alias = "author")]
        [JsonProperty("author")]
        public UserInfo Author { get; set; }

        [YamlMember(Alias = "id")]
        [JsonProperty("id")]
        public string CommitId { get; set; }

        [YamlMember(Alias = "message")]
        [JsonProperty("message")]
        public string ShortMessage { get; set; }
    }
}
