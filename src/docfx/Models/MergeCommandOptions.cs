// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.DocAsCode;

[Verb("merge", HelpText = "Merge .net base API in YAML files and toc files.")]
internal class MergeCommandOptions : LogOptions
{
    public string OutputFolder { get; set; }

    [Value(0, MetaName = "config", HelpText = "Path to docfx.json")]
    public string ConfigFile { get; set; }

    [Option("content", Separator = ',', HelpText = "Specifies content files for generating documentation.")]
    public IEnumerable<string> Content { get; set; }

    [Option("globalMetadata", HelpText = "Specify global metadata key-value pair in json format. It overrides the globalMetadata settings from the config file.")]
    public string GlobalMetadata { get; set; }

    [Option("globalMetadataFile", HelpText = "Specify a JSON file path containing globalMetadata settings, as similar to {\"globalMetadata\":{\"key\":\"value\"}}. It overrides the globalMetadata settings from the config file.")]
    public string GlobalMetadataFilePath { get; set; }

    [Option("fileMetadataFile", HelpText = "Specify a JSON file path containing fileMetadata settings, as similar to {\"fileMetadata\":{\"key\":\"value\"}}. It overrides the fileMetadata settings from the config file.")]
    public string FileMetadataFilePath { get; set; }

    [Option("tocMetadata", Separator = ',', HelpText = "Specify metadata names that need to be merged into toc file")]
    public IEnumerable<string> TocMetadata { get; set; }
}
