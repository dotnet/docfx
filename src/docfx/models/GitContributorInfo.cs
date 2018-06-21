// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class GitContributorInfo
    {
        [JsonProperty("author")]
        public GitUserInfo Author { get; set; }

        [JsonProperty("contributors")]
        public GitUserInfo[] Contributors { get; set; }

        [JsonProperty("update_at")]
        public string UpdatedAt { get; set; }

        [JsonIgnore]
        public DateTime UpdatedAtDateTime { get; set; }
    }
}
