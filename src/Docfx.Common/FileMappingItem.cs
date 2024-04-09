// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx;

/// <summary>
/// Data model for a file-mapping item
/// </summary>
public class FileMappingItem
{
    /// <summary>
    /// The name of current item, the value is not used for now
    /// </summary>
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The file glob pattern collection, with path relative to property `src` is value is set
    /// </summary>
    [JsonProperty("files")]
    [JsonPropertyName("files")]
    public FileItems Files { get; set; }

    /// <summary>
    /// The file glob pattern collection for files that should be excluded, with path relative to property `src` is value is set
    /// </summary>
    [JsonProperty("exclude")]
    [JsonPropertyName("exclude")]
    public FileItems Exclude { get; set; }

    /// <summary>
    /// `src` defines the root folder for the source files.
    /// </summary>
    [JsonProperty("src")]
    [JsonPropertyName("src")]
    public string Src { get; set; }

    /// <summary>
    /// The destination folder for the files if copy/transform is used
    /// </summary>
    [JsonProperty("dest")]
    [JsonPropertyName("dest")]
    public string Dest { get; set; }

    /// <summary>
    /// Group name for the current file-mapping item.
    /// If not set, treat the current file-mapping item as in default group.
    /// Mappings with the same group name will be built together.
    /// Cross reference doesn't support cross different groups.
    /// </summary>
    [JsonProperty("group")]
    [JsonPropertyName("group")]
    public string Group { get; set; }

    /// <summary>
    /// The Root TOC Path used for navbar in current group, relative to output root.
    /// If not set, will use the toc in output root in current group if exists.
    /// </summary>
    [JsonProperty("rootTocPath")]
    [JsonPropertyName("rootTocPath")]
    public string RootTocPath { get; set; }

    /// <summary>
    /// Pattern match will be case sensitive.
    /// By default the pattern is case insensitive
    /// </summary>
    [JsonProperty("case")]
    [JsonPropertyName("case")]
    public bool? Case { get; set; }

    /// <summary>
    /// Disable pattern begin with `!` to mean negate
    /// By default the usage is enabled.
    /// </summary>
    [JsonProperty("noNegate")]
    [JsonPropertyName("noNegate")]
    public bool? DisableNegate { get; set; }

    /// <summary>
    /// Disable `{a,b}c` => `["ac", "bc"]`.
    /// By default the usage is enabled.
    /// </summary>
    [JsonProperty("noExpand")]
    [JsonPropertyName("noExpand")]
    public bool? NoExpand { get; set; }

    /// <summary>
    /// Disable the usage of `\` to escape values.
    /// By default the usage is enabled.
    /// </summary>
    [JsonProperty("noEscape")]
    [JsonPropertyName("noEscape")]
    public bool? NoEscape { get; set; }

    /// <summary>
    /// Disable the usage of `**` to match everything including `/` when it is the beginning of the pattern or is after `/`.
    /// By default the usage is enable.
    /// </summary>
    [JsonProperty("noGlobStar")]
    [JsonPropertyName("noGlobStar")]
    public bool? NoGlobStar { get; set; }

    /// <summary>
    /// Allow files start with `.` to be matched even if `.` is not explicitly specified in the pattern.
    /// By default files start with `.` will not be matched by `*` unless the pattern starts with `.`.
    /// </summary>
    [JsonProperty("dot")]
    [JsonPropertyName("dot")]
    public bool? Dot { get; set; }

    public FileMappingItem() { }

    public FileMappingItem(params string[] files)
    {
        Files = new FileItems(files);
    }
}
