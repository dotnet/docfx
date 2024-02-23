// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx.Plugins;

public class SitemapElementOptions
{
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; }

    [JsonPropertyName("changefreq")]
    public PageChangeFrequency? ChangeFrequency { get; set; }

    [JsonPropertyName("priority")]
    public double? Priority { get; set; }

    [JsonPropertyName("lastmod")]
    public DateTime? LastModified { get; set; }
}
