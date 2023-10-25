// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Build.Engine;

internal sealed class SystemMetadata
{
    [JsonProperty("_title")]
    [JsonPropertyName("_title")]
    public string Title { get; set; }

    [JsonProperty("_tocTitle")]
    [JsonPropertyName("_tocTitle")]
    public string TocTitle { get; set; }

    [JsonProperty("_name")]
    [JsonPropertyName("_name")]
    public string Name { get; set; }

    [JsonProperty("_description")]
    [JsonPropertyName("_description")]
    public string Description { get; set; }

    /// <summary>
    /// TOC PATH from ~ ROOT
    /// </summary>
    [JsonProperty("_tocPath")]
    [JsonPropertyName("_tocPath")]
    public string TocPath { get; set; }

    /// <summary>
    /// ROOT TOC PATH from ~ ROOT
    /// </summary>
    [JsonProperty("_navPath")]
    [JsonPropertyName("_navPath")]
    public string RootTocPath { get; set; }

    /// <summary>
    /// Current file's relative path to ROOT, e.g. file is ~/A/B.md, relative path to ROOT is ../
    /// </summary>
    [JsonProperty("_rel")]
    [JsonPropertyName("_rel")]
    public string RelativePathToRoot { get; set; }

    /// <summary>
    /// Current file's path from ~ ROOT
    /// </summary>
    [JsonProperty("_path")]
    [JsonPropertyName("_path")]
    public string Path { get; set; }

    /// <summary>
    /// Current file's key from ~ ROOT
    /// </summary>
    [JsonProperty("_key")]
    [JsonPropertyName("_key")]
    public string Key { get; set; }

    /// <summary>
    /// Current file's relative path to ROOT TOC file
    /// </summary>
    [JsonProperty("_navRel")]
    [JsonPropertyName("_navRel")]
    public string RelativePathToRootToc { get; set; }

    /// <summary>
    /// Current file's relative path to current file's TOC file
    /// </summary>
    [JsonProperty("_tocRel")]
    [JsonPropertyName("_tocRel")]
    public string RelativePathToToc { get; set; }

    /// <summary>
    /// The file key for Root TOC file, starting with `~`
    /// </summary>
    [JsonProperty("_navKey")]
    [JsonPropertyName("_navKey")]
    public string RootTocKey { get; set; }

    /// <summary>
    /// The file key for current file's TOC file, starting with `~`
    /// </summary>
    [JsonProperty("_tocKey")]
    [JsonPropertyName("_tocKey")]
    public string TocKey { get; set; }

    /// <summary>
    /// Current file's version name
    /// </summary>
    [JsonProperty("_version")]
    [JsonPropertyName("_version")]
    public string VersionName { get; set; }

    /// <summary>
    /// Current file's version root path from ~ ROOT
    /// </summary>
    [JsonProperty("_versionPath")]
    [JsonPropertyName("_versionPath")]
    public string VersionFolder { get; set; }
}
