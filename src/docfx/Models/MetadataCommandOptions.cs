// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

[Description("Generate YAML files from source code")]
internal class MetadataCommandOptions : LogOptions
{
    [Description("Skip to markup the triple slash comments")]
    [CommandOption("--shouldSkipMarkup")]
    public bool ShouldSkipMarkup { get; set; }

    [Description("Specify the output base directory")]
    [CommandOption("-o|--output")]
    public string OutputFolder { get; set; }

    [Description("Path to docfx.json")]
    [CommandArgument(0, "[config]")]
    public string Config { get; set; }

    [Description("Specify the filter config file")]
    [CommandOption("--filter")]
    public string FilterConfigFile { get; set; }

    [Description("Specify the name to use for the global namespace")]
    [CommandOption("--globalNamespaceId")]
    public string GlobalNamespaceId { get; set; }

    [Description("--property <n1>=<v1>;<n2>=<v2> An optional set of MSBuild properties used when interpreting project files. These are the same properties that are passed to msbuild via the /property:<n1>=<v1>;<n2>=<v2> command line argument")]
    [CommandOption("--property")]
    public string MSBuildProperties { get; set; }

    [Description("Disable fetching Git related information for articles. By default it is enabled and may have side effect on performance when the repo is large.")]
    [CommandOption("--disableGitFeatures")]
    public bool DisableGitFeatures { get; set; }

    [Description("Disable the default API filter (default filter only generate public or protected APIs).")]
    [CommandOption("--disableDefaultFilter")]
    public bool DisableDefaultFilter { get; set; }

    [Description("Determines the namespace layout in table of contents.")]
    [CommandOption("--namespaceLayout")]
    public NamespaceLayout? NamespaceLayout { get; set; }
}
