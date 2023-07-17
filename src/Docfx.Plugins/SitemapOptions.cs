// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx.Plugins;

[Serializable]
public class SitemapOptions : SitemapElementOptions
{
    [JsonProperty("fileOptions")]
    [JsonConverter(typeof(DictionaryAsListJsonConverter<SitemapElementOptions>))]
    public IList<KeyValuePair<string, SitemapElementOptions>> FileOptions { get; set; }
}
