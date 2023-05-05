// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Newtonsoft.Json;

namespace Microsoft.DocAsCode.Plugins;

public class MarkdownServiceProperties
{
    /// <summary>
    /// Enables line numbers.
    /// </summary>
    [JsonProperty("EnableSourceInfo")]
    public bool EnableSourceInfo { get; set; } = true;

    /// <summary>
    /// Contains a list of optional Markdig extensions that are not
    /// enabled by default by DocFX.
    /// </summary>
    [JsonProperty("markdigExtensions")]
    public string[] MarkdigExtensions { get; set; }

    [JsonProperty("fallbackFolders")]
    public string[] FallbackFolders { get; set; }

    /// <summary>
    /// Alert keywords in markdown without the surrounding [!] and the corresponding CSS class names.
    /// E.g., TIP -> alert alert-info
    /// </summary>
    [JsonProperty("alerts")]
    public Dictionary<string, string> Alerts { get; set; }
}

public class MarkdownServiceParameters
{
    public string BasePath { get; set; }
    public string TemplateDir { get; set; }
    public MarkdownServiceProperties Extensions { get; set; } = new();
    public ImmutableDictionary<string, string> Tokens { get; set; } = ImmutableDictionary<string, string>.Empty;
}
