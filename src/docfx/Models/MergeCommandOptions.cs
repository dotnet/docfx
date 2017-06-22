// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using CommandLine;

    [OptionUsage("merge [<config file path>]")]
    internal class MergeCommandOptions : LogOptions, ICanPrintHelpMessage
    {
        public string OutputFolder { get; set; }

        [ValueOption(0)]
        public string ConfigFile { get; set; }

        [Option('h', "help", HelpText = "Print help message for this sub-command")]
        public bool PrintHelpMessage { get; set; }

        [OptionList("content", Separator = ',', HelpText = "Specifies content files for generating documentation.")]
        public List<string> Content { get; set; }

        [Option("globalMetadata", HelpText = "Specify global metadata key-value pair in json format. It overrides the globalMetadata settings from the config file.")]
        public string GlobalMetadata { get; set; }

        [Option("globalMetadataFile", HelpText = "Specify a JSON file path containing globalMetadata settings, as similar to {\"globalMetadata\":{\"key\":\"value\"}}. It overrides the globalMetadata settings from the config file.")]
        public string GlobalMetadataFilePath { get; set; }

        [Option("fileMetadataFile", HelpText = "Specify a JSON file path containing fileMetadata settings, as similar to {\"fileMetadata\":{\"key\":\"value\"}}. It overrides the fileMetadata settings from the config file.")]
        public string FileMetadataFilePath { get; set; }

        [OptionList("tocMetadata", Separator = ',', HelpText = "Specify metadata names that need to be merged into toc file")]
        public List<string> TocMetadata { get; set; }
    }
}
