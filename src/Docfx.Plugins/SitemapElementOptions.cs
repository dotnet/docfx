// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Plugins;

public class SitemapElementOptions
{
    [JsonProperty("baseUrl")]
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; }

    [JsonProperty("changefreq")]
    [JsonPropertyName("changefreq")]
    public PageChangeFrequency? ChangeFrequency { get; set; }

    [JsonProperty("priority")]
    [JsonPropertyName("priority")]
    public double? Priority { get; set; }

    [JsonProperty("lastmod")]
    [JsonPropertyName("lastmod")]
    public DateTime? LastModified { get; set; }
}
