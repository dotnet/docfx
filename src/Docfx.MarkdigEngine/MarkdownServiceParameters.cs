// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Docfx.MarkdigEngine.Extensions;
using Newtonsoft.Json;

namespace Docfx.Plugins;

public class MarkdownServiceProperties
{
    /// <summary>
    /// Enables line numbers.
    /// </summary>
    [JsonProperty("enableSourceInfo")]
    [JsonPropertyName("enableSourceInfo")]
    public bool EnableSourceInfo { get; set; } = true;

    /// <summary>
    /// List of optional Markdig extensions to add or modify settings.
    /// If extension is specified by name. Markdig extension will be added with default configuration.
    /// If extension name is specified name with options. Add or Replace markdig extensions with specified options.
    /// </summary>
    [JsonProperty("markdigExtensions")]
    [JsonPropertyName("markdigExtensions")]
    public MarkdigExtensionSetting[] MarkdigExtensions { get; set; }

    [JsonProperty("fallbackFolders")]
    [JsonPropertyName("fallbackFolders")]
    public string[] FallbackFolders { get; set; }

    /// <summary>
    /// Alert keywords in markdown without the surrounding [!] and the corresponding CSS class names.
    /// E.g., TIP -> alert alert-info
    /// </summary>
    [JsonProperty("alerts")]
    [JsonPropertyName("alerts")]
    public Dictionary<string, string> Alerts { get; set; }

    /// <summary>
    /// PlantUml extension configuration parameters
    /// </summary>
    [JsonProperty("plantUml")]
    [JsonPropertyName("plantUml")]
    public PlantUmlOptions PlantUml { get; set; }
}

public class MarkdownServiceParameters
{
    public string BasePath { get; set; }
    public string TemplateDir { get; set; }
    public MarkdownServiceProperties Extensions { get; set; } = new();
    public ImmutableDictionary<string, string> Tokens { get; set; } = ImmutableDictionary<string, string>.Empty;
}
