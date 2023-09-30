// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Docfx.Plugins;

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
