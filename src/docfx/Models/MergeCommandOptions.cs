// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

[Description("Merge .net base API in YAML files and toc files.")]
internal class MergeCommandOptions : LogOptions
{
    [Description("Specify the output folder")]
    [CommandOption("-o|--output")]
    public string OutputFolder { get; set; }

    [Description("Path to docfx.json")]
    [CommandArgument(0, "[config]")]
    public string Config { get; set; }

    [Description("Specifies content files for generating documentation.")]
    [CommandOption("--content")]
    public IEnumerable<string> Content { get; set; }

    [Description("Specify global metadata key-value pair in json format. It overrides the globalMetadata settings from the config file.")]
    [CommandOption("--globalMetadata")]
    public string GlobalMetadata { get; set; }

    [Description("Specify a JSON file path containing globalMetadata settings, as similar to {\"globalMetadata\":{\"key\":\"value\"}}. It overrides the globalMetadata settings from the config file.")]
    [CommandOption("--globalMetadataFile")]
    public string GlobalMetadataFilePath { get; set; }

    [Description("Specify a JSON file path containing fileMetadata settings, as similar to {\"fileMetadata\":{\"key\":\"value\"}}. It overrides the fileMetadata settings from the config file.")]
    [CommandOption("--fileMetadataFile")]
    public string FileMetadataFilePath { get; set; }

    [Description("Specify metadata names that need to be merged into toc file")]
    [CommandOption("--tocMetadata")]
    public IEnumerable<string> TocMetadata { get; set; }
}
