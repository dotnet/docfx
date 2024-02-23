// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx.Plugins;

public class SitemapOptions : SitemapElementOptions
{
    [JsonPropertyName("fileOptions")]
    public Dictionary<string, SitemapElementOptions> FileOptions { get; set; }
}
