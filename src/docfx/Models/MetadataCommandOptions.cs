// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Docfx;

[Description("Generate YAML files from source code")]
internal class MetadataCommandOptions : LogOptions
{
    [Description("Skip to markup the triple slash comments")]
    [CommandOption("--shouldSkipMarkup")]
    public bool ShouldSkipMarkup { get; set; }

    [Description("Specify the output base directory")]
    [CommandOption("-o|--output")]
    public string OutputFolder { get; set; }

    [Description("Specify the output type")]
    [CommandOption("--outputFormat")]
    public MetadataOutputFormat? OutputFormat { get; set; }

    [Description("Path to docfx.json")]
    [CommandArgument(0, "[config]")]
    public string Config { get; set; }

    [Description("Specify the filter config file")]
    [CommandOption("--filter")]
    public string FilterConfigFile { get; set; }

    [Description("Specify the name to use for the global namespace")]
    [CommandOption("--globalNamespaceId")]
    public string GlobalNamespaceId { get; set; }

    [Description("--property <n1>=<v1>;<n2>=<v2> An optional set of MSBuild properties used when interpreting project files. These are the same properties that are passed to MSBuild via the /property:<n1>=<v1>;<n2>=<v2> command line argument")]
    [CommandOption("--property")]
    public string MSBuildProperties { get; set; }

    [Description("Disable fetching Git related information for articles. By default it is enabled and may have side effect on performance when the repo is large.")]
    [CommandOption("--disableGitFeatures")]
    public bool DisableGitFeatures { get; set; }

    [Description("Disable the default API filter (default filter only generate public or protected APIs).")]
    [CommandOption("--disableDefaultFilter")]
    public bool DisableDefaultFilter { get; set; }

    [Description("Do not run `dotnet restore` before building the projects")]
    [CommandOption("--noRestore")]
    public bool NoRestore { get; set; }

    [Description("Determines the category layout in table of contents.")]
    [CommandOption("--categoryLayout")]
    public CategoryLayout? CategoryLayout { get; set; }

    [Description("Determines the namespace layout in table of contents.")]
    [CommandOption("--namespaceLayout")]
    public NamespaceLayout? NamespaceLayout { get; set; }

    [Description("Determines the member page layout.")]
    [CommandOption("--memberLayout")]
    public MemberLayout? MemberLayout { get; set; }
}
