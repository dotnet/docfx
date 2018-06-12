// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class LegacyCrossRepoReferenceInfoItem
    {
        [JsonProperty("path_to_root")]
        public string PathToRoot { get; set; }

        [JsonProperty("branch")]
        public string Branch { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
