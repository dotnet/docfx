// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace TripleCrownValidation
{
    public class XRefResult
    {
        [JsonProperty("uid")]
        public string Uid { get; set; }
        [JsonProperty("href")]
        public string Href { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
