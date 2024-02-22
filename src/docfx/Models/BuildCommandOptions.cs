// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Docfx;

[Description("Generate client-only website combining API in YAML files and conceptual files")]
internal class BuildCommandOptions : LogOptions
{
    [Description("Specify the output base directory")]
    [CommandOption("-o|--output")]
    public string OutputFolder { get; set; }

    [Description("Path to docfx.json")]
    [CommandArgument(0, "[config]")]
    public string ConfigFile { get; set; }

    [Description("Specify a list of global metadata in key value pairs (e.g. --metadata _appTitle=\"My App\" --metadata _disableContribution)")]
    [CommandOption("-m|--metadata")]
    public string[] Metadata { get; set; }

    [Description("Specify the urls of xrefmap used by content files.")]
    [CommandOption("-x|--xref")]
    [TypeConverter(typeof(ArrayOptionConverter))]
    public IEnumerable<string> XRefMaps { get; set; }

    [Description("Specify the template name to apply to. If not specified, output YAML file will not be transformed.")]
    [CommandOption("-t|--template")]
    [TypeConverter(typeof(ArrayOptionConverter))]
    public IEnumerable<string> Templates { get; set; }

    [Description("Specify which theme to use. By default 'default' theme is offered.")]
    [CommandOption("--theme")]
    [TypeConverter(typeof(ArrayOptionConverter))]
    public IEnumerable<string> Themes { get; set; }

    [Description("Host the generated documentation to a website")]
    [CommandOption("-s|--serve")]
    public bool Serve { get; set; }

    [Description("Specify the hostname of the hosted website (e.g., 'localhost' or '*')")]
    [CommandOption("-n|--hostname")]
    public string Host { get; set; }

    [Description("Specify the port of the hosted website")]
    [CommandOption("-p|--port")]
    public int? Port { get; set; }

    [Description("Open a web browser when the hosted website starts.")]
    [CommandOption("--open-browser")]
    public bool OpenBrowser { get; set; }

    [Description("Open a file in a web browser when the hosted website starts.")]
    [CommandOption("--open-file <RELATIVE_PATH>")]
    public string OpenFile { get; set; }

    [Description("Run in debug mode. With debug mode, raw model and view model will be exported automatically when it encounters error when applying templates. If not specified, it is false.")]
    [CommandOption("--debug")]
    public bool EnableDebugMode { get; set; }

    [Description("The output folder for files generated for debugging purpose when in debug mode. If not specified, it is ${TempPath}/docfx")]
    [CommandOption("--debugOutput")]
    public string OutputFolderForDebugFiles { get; set; }

    [Description("If set to true, data model to run template script will be extracted in .raw.model.json extension")]
    [CommandOption("--exportRawModel")]
    public bool ExportRawModel { get; set; }

    [Description("Specify the output folder for the raw model. If not set, the raw model will be generated to the same folder as the output documentation")]
    [CommandOption("--rawModelOutputFolder")]
    public string RawModelOutputFolder { get; set; }

    [Description("Specify the output folder for the view model. If not set, the view model will be generated to the same folder as the output documentation")]
    [CommandOption("--viewModelOutputFolder")]
    public string ViewModelOutputFolder { get; set; }

    [Description("If set to true, data model to apply template will be extracted in .view.model.json extension")]
    [CommandOption("--exportViewModel")]
    public bool ExportViewModel { get; set; }

    [Description("If set to true, template will not be actually applied to the documents. This option is always used with --exportRawModel or --exportViewModel is set so that only raw model files or view model files are generated.")]
    [CommandOption("--dryRun")]
    public bool DryRun { get; set; }

    [Description("Set the max parallelism, 0 is auto.")]
    [CommandOption("--maxParallelism")]
    public int? MaxParallelism { get; set; }

    [Description("Set the parameters for markdown engine, value should be a JSON string.")]
    [CommandOption("--markdownEngineProperties")]
    public string MarkdownEngineProperties { get; set; }

    [Description("Set the order of post processors in plugins")]
    [CommandOption("--postProcessors")]
    [TypeConverter(typeof(ArrayOptionConverter))]
    public IEnumerable<string> PostProcessors { get; set; }

    [Description("Disable fetching Git related information for articles. By default it is enabled and may have side effect on performance when the repo is large.")]
    [CommandOption("--disableGitFeatures")]
    public bool DisableGitFeatures { get; set; }
}
