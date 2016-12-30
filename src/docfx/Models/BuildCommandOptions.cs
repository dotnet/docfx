// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using CommandLine;

    using Microsoft.DocAsCode.Common;

    [OptionUsage("build [<config file path>]")]
    internal class BuildCommandOptions : ICanPrintHelpMessage, ILoggable
    {
        [Option('o', "output")]
        public string OutputFolder { get; set; }

        [ValueOption(0)]
        public string ConfigFile { get; set; }

        [Option('h', "help", HelpText = "Print help message for this sub-command")]
        public bool PrintHelpMessage { get; set; }

        [Option('l', "log", HelpText = "Specify the file name to save processing log")]
        public string LogFilePath { get; set; }

        [Option("logLevel", HelpText = "Specify to which log level will be logged. By default log level >= Info will be logged. The acceptable value could be Verbose, Info, Warning, Error.")]
        public LogLevel? LogLevel { get; set; }

        [Option("repositoryRoot", HelpText = "Specify the GIT repository root folder.")]
        public string RepoRoot { get; set; }

        [OptionList("content", Separator = ',', HelpText = "Specify content files for generating documentation.")]
        public List<string> Content { get; set; }

        [OptionList("resource", Separator = ',', HelpText = "Specify resources used by content files.")]
        public List<string> Resource { get; set; }

        [OptionList("overwrite", Separator = ',', HelpText = "Specify overwrite files used by content files.")]
        public List<string> Overwrite { get; set; }

        [OptionList("externalReference", Separator = ',', HelpText = "Specify external reference files used by content files.")]
        public List<string> ExternalReference { get; set; }

        [OptionList('x', "xref", Separator = ',', HelpText = "Specify the urls of xrefmap used by content files.")]
        public List<string> XRefMaps { get; set; }

        [OptionList('t', "template", Separator = ',', HelpText = "Specify the template name to apply to. If not specified, output YAML file will not be transformed.")]
        public List<string> Templates { get; set; }

        [OptionList("theme", Separator = ',', HelpText = "Specify which theme to use. By default 'default' theme is offered.")]
        public List<string> Themes { get; set; }

        [Option('s', "serve", HelpText = "Host the generated documentation to a website")]
        public bool Serve { get; set; }

        [Option('p', "port", HelpText = "Specify the port of the hosted website")]
        public int? Port { get; set; }

        [Option('f', "force", HelpText = "Force re-build all the documentation")]
        public bool ForceRebuild { get; set; }

        [Option("forcePostProcess", HelpText = "Force to re-process the documentation in post processors. It will be cascaded from force option.")]
        public bool ForcePostProcess { get; set; }

        [Option("globalMetadata", HelpText = "Specify global metadata key-value pair in json format. It overrides the globalMetadata settings from the config file.")]
        public string GlobalMetadata { get; set; }

        [Option("globalMetadataFile", HelpText = "Specify a JSON file path containing globalMetadata settings, as similar to {\"globalMetadata\":{\"key\":\"value\"}}. It overrides the globalMetadata settings from the config file.")]
        public string GlobalMetadataFilePath { get; set; }

        [OptionList("globalMetadataFiles", Separator = ',', HelpText = "Specify a list of JSON file path containing globalMetadata settings, as similar to {\"key\":\"value\"}. It overrides the globalMetadata settings from the config file.")]
        public List<string> GlobalMetadataFilePaths { get; set; }

        [Option("fileMetadataFile", HelpText = "Specify a JSON file path containing fileMetadata settings, as similar to {\"fileMetadata\":{\"key\":\"value\"}}. It overrides the fileMetadata settings from the config file.")]
        public string FileMetadataFilePath { get; set; }

        [OptionList("fileMetadataFiles", Separator = ',', HelpText = "Specify a list of JSON file path containing fileMetadata settings, as similar to {\"key\":\"value\"}. It overrides the fileMetadata settings from the config file.")]
        public List<string> FileMetadataFilePaths { get; set; }

        [Option("exportRawModel", HelpText = "If set to true, data model to run template script will be extracted in .raw.model.json extension")]
        public bool ExportRawModel { get; set; }

        [Option("rawModelOutputFolder", HelpText = "Specify the output folder for the raw model. If not set, the raw model will be generated to the same folder as the output documentation")]
        public string RawModelOutputFolder { get; set; }

        [Option("viewModelOutputFolder", HelpText = "Specify the output folder for the view model. If not set, the view model will be generated to the same folder as the output documentation")]
        public string ViewModelOutputFolder { get; set; }

        [Option("exportViewModel", HelpText = "If set to true, data model to apply template will be extracted in .view.model.json extension")]
        public bool ExportViewModel { get; set; }

        [Option("dryRun", HelpText = "If set to true, template will not be actually applied to the documents. This option is always used with --exportRawModel or --exportViewModel is set so that only raw model files or view model files are generated.")]
        public bool DryRun { get; set; }

        [Option("maxParallelism", HelpText = "Set the max parallelism, 0 is auto.")]
        public int? MaxParallelism { get; set; }

        [Option("markdownEngineName", HelpText = "Set the name of markdown engine, default is 'dfm'.")]
        public string MarkdownEngineName { get; set; }

        [Option("markdownEngineProperties", HelpText = "Set the parameters for markdown engine, value should be a JSON string.")]
        public string MarkdownEngineProperties { get; set; }

        [Option("noLangKeyword", HelpText = "Disable default lang keyword.")]
        public bool? NoLangKeyword { get; set; }

        [Option("intermediateFolder", HelpText = "Set folder for intermediate build results.")]
        public string IntermediateFolder { get; set; }

        [Option("changesFile", HelpText = "Set changes file.")]
        public string ChangesFile { get; set; }

        [OptionList("postProcessors", Separator = ',', HelpText = "Set the order of post processors in plugins")]
        public List<string> PostProcessors { get; set; }
    }
}
