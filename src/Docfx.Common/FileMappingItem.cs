// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Docfx;

/// <summary>
/// Data model for a file-mapping item
/// </summary>
public class FileMappingItem
{
    /// <summary>
    /// The name of current item, the value is not used for now
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The file glob pattern collection, with path relative to property `src`/`cwd` is value is set
    /// </summary>
    [JsonPropertyName("files")]
    public FileItems Files { get; set; }

    /// <summary>
    /// The file glob pattern collection for files that should be excluded, with path relative to property `src`/`cwd` is value is set
    /// </summary>
    [JsonPropertyName("exclude")]
    public FileItems Exclude { get; set; }

    /// <summary>
    /// `src` defines the root folder for the source files, it has the same meaning as `cwd`
    /// </summary>
    [JsonPropertyName("src")]
    public string Src { get; set; }

    /// <summary>
    /// The destination folder for the files if copy/transform is used
    /// </summary>
    [JsonPropertyName("dest")]
    public string Dest { get; set; }

    /// <summary>
    /// Group name for the current file-mapping item.
    /// If not set, treat the current file-mapping item as in default group.
    /// Mappings with the same group name will be built together.
    /// Cross reference doesn't support cross different groups.
    /// </summary>
    [JsonPropertyName("group")]
    public string Group { get; set; }

    /// <summary>
    /// The Root TOC Path used for navbar in current group, relative to output root.
    /// If not set, will use the toc in output root in current group if exists.
    /// </summary>
    [JsonPropertyName("rootTocPath")]
    public string RootTocPath { get; set; }

    /// <summary>
    /// Pattern match will be case sensitive.
    /// By default the pattern is case insensitive
    /// </summary>
    [JsonPropertyName("case")]
    public bool? Case { get; set; }

    /// <summary>
    /// Disable pattern begin with `!` to mean negate
    /// By default the usage is enabled.
    /// </summary>
    [JsonPropertyName("noNegate")]
    public bool? DisableNegate { get; set; }

    /// <summary>
    /// Disable `{a,b}c` => `["ac", "bc"]`.
    /// By default the usage is enabled.
    /// </summary>
    [JsonPropertyName("noExpand")]
    public bool? NoExpand { get; set; }

    /// <summary>
    /// Disable the usage of `\` to escape values.
    /// By default the usage is enabled.
    /// </summary>
    [JsonPropertyName("noEscape")]
    public bool? NoEscape { get; set; }

    /// <summary>
    /// Disable the usage of `**` to match everything including `/` when it is the beginning of the pattern or is after `/`.
    /// By default the usage is enable.
    /// </summary>
    [JsonPropertyName("noGlobStar")]
    public bool? NoGlobStar { get; set; }

    /// <summary>
    /// Allow files start with `.` to be matched even if `.` is not explicitly specified in the pattern.
    /// By default files start with `.` will not be matched by `*` unless the pattern starts with `.`.
    /// </summary>
    [JsonPropertyName("dot")]
    public bool? Dot { get; set; }

    public FileMappingItem() { }

    public FileMappingItem(params string[] files)
    {
        Files = new FileItems(files);
    }
}
