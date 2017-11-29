// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using CommandLine;

    [OptionUsage("build [<config file path>]")]
    internal class BuildCommandOptions : LogOptions, ICanPrintHelpMessage
    {
        [Option('o', "output", HelpText = "Specify the output base directory")]
        public string OutputFolder { get; set; }

        [ValueOption(0)]
        public string ConfigFile { get; set; }

        [Option('h', "help", HelpText = "Print help message for this sub-command")]
        public bool PrintHelpMessage { get; set; }

        [OptionList("content", Separator = ',', HelpText = "Specify content files for generating documentation.")]
        public List<string> Content { get; set; }

        [OptionList("resource", Separator = ',', HelpText = "Specify resources used by content files.")]
        public List<string> Resource { get; set; }

        [OptionList("overwrite", Separator = ',', HelpText = "Specify overwrite files used by content files.")]
        public List<string> Overwrite { get; set; }

        [OptionList('x', "xref", Separator = ',', HelpText = "Specify the urls of xrefmap used by content files.")]
        public List<string> XRefMaps { get; set; }

        [OptionList('t', "template", Separator = ',', HelpText = "Specify the template name to apply to. If not specified, output YAML file will not be transformed.")]
        public List<string> Templates { get; set; }

        [OptionList("theme", Separator = ',', HelpText = "Specify which theme to use. By default 'default' theme is offered.")]
        public List<string> Themes { get; set; }

        [Option('s', "serve", HelpText = "Host the generated documentation to a website")]
        public bool Serve { get; set; }

        [Option('n', "hostname", HelpText = "Specify the hostname of the hosted website (e.g., 'localhost' or '*')")]
        public string Host { get; set; }

        [Option('p', "port", HelpText = "Specify the port of the hosted website")]
        public int? Port { get; set; }

        [Option('f', "force", HelpText = "Force re-build all the documentation")]
        public bool ForceRebuild { get; set; }

        [Option("debug", HelpText = "Run in debug mode. With debug mode, raw model and view model will be exported automatically when it encounters error when applying templates. If not specified, it is false.")]
        public bool EnableDebugMode { get; set; }

        [Option("debugOutput", HelpText = "The output folder for files generated for debugging purpose when in debug mode. If not specified, it is ${TempPath}/docfx")]
        public string OutputFolderForDebugFiles { get; set; }

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

        [Option("lruSize", HelpText = "Set the LRU cached model count (approximately the same as the count of input files). By default, it is 8192 for 64bit and 3072 for 32bit process. With LRU cache enabled, memory usage decreases and time consumed increases. If set to 0, Lru cache is disabled.")]
        public int? LruSize { get; set; }

        [Option("keepFileLink", HelpText = "If set to true, docfx does not dereference (aka. copy) file to the output folder, instead, it saves a link_to_path property inside mainfiest.json to indicate the physical location of that file.")]
        public bool KeepFileLink { get; set; }

        [Option("cleanupCacheHistory", HelpText = "If set to true, docfx create a new intermediate folder for cache files, historical cache data will be cleaned up")]
        public bool CleanupCacheHistory { get; set; }

        [Option("falName", HelpText = "Set the name of input file abstract layer builder.")]
        public string FALName { get; set; }

        [Option("disableGitFeatures", HelpText = "Disable fetching Git related information for articles. By default it is enabled and may have side effect on performance when the repo is large.")]
        public bool DisableGitFeatures { get; set; }

        [Option("schemaLicense", HelpText = "Please provide the license key for validating schema using NewtonsoftJson.Schema here.")]
        public string SchemaLicense { get; set; }
    }
}
