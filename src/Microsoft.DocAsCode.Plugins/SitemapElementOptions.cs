// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    
    using Newtonsoft.Json;

    [Serializable]
    public class SitemapElementOptions
    {
        [JsonProperty("baseUrl")]
        public string BaseUrl { get; set; }

        [JsonProperty("changefreq")]
        public PageChangeFrequency? ChangeFrequency { get; set; }

        [JsonProperty("priority")]
        public double? Priority { get; set; }

        [JsonProperty("lastmod")]
        public DateTime? LastModified { get; set; }
    }
}
