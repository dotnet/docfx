// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx.Build.Engine;

class SystemMetadata
{

    /// <summary>
    /// TOC PATH from ~ ROOT
    /// </summary>
    [JsonPropertyName("_tocPath")]
    public string TocPath { get; set; }

    /// <summary>
    /// ROOT TOC PATH from ~ ROOT
    /// </summary>
    [JsonPropertyName("_navPath")]
    public string NavPath { get; set; }

    /// <summary>
    /// Current file's relative path to ROOT, e.g. file is ~/A/B.md, relative path to ROOT is ../
    /// </summary>
    [JsonPropertyName("_rel")]
    public string Rel { get; set; }

    /// <summary>
    /// Current file's path from ~ ROOT
    /// </summary>
    [JsonPropertyName("_path")]
    public string Path { get; set; }

    /// <summary>
    /// Current file's key from ~ ROOT
    /// </summary>
    [JsonPropertyName("_key")]
    public string Key { get; set; }

    /// <summary>
    /// Current file's relative path to ROOT TOC file
    /// </summary>
    [JsonPropertyName("_navRel")]
    public string NavRel { get; set; }

    /// <summary>
    /// Current file's relative path to current file's TOC file
    /// </summary>
    [JsonPropertyName("_tocRel")]
    public string TocRel { get; set; }

    /// <summary>
    /// The file key for Root TOC file, starting with `~`
    /// </summary>
    [JsonPropertyName("_navKey")]
    public string NavKey { get; set; }

    /// <summary>
    /// The file key for current file's TOC file, starting with `~`
    /// </summary>
    [JsonPropertyName("_tocKey")]
    public string TocKey { get; set; }

    /// <summary>
    /// Current file's version name
    /// </summary>
    [JsonPropertyName("_version")]
    public string Version { get; set; }

    /// <summary>
    /// Current file's version root path from ~ ROOT
    /// </summary>
    [JsonPropertyName("_versionPath")]
    public string VersionPath { get; set; }
}
