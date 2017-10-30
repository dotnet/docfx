// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [Serializable]
    public class SitemapOptions : SitemapElementOptions
    {
        [JsonProperty("fileOptions")]
        [JsonConverter(typeof(DictionaryAsListJsonConverter<SitemapElementOptions>))]
        public IList<KeyValuePair<string, SitemapElementOptions>> FileOptions { get; set; }
    }
}
