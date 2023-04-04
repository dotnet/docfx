// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.DocAsCode;

[Verb("metadata", HelpText = "Generate YAML files from source code")]
internal class MetadataCommandOptions : LogOptions
{
    [Option("shouldSkipMarkup", HelpText = "Skip to markup the triple slash comments")]
    public bool ShouldSkipMarkup { get; set; }

    [Option('o', "output", HelpText = "Specify the output base directory")]
    public string OutputFolder { get; set; }

    [Value(0, MetaName = "config", HelpText = "Path to docfx.json")]
    public IEnumerable<string> Projects { get; set; }

    [Option("filter", HelpText = "Specify the filter config file")]
    public string FilterConfigFile { get; set; }

    [Option("globalNamespaceId", HelpText = "Specify the name to use for the global namespace")]
    public string GlobalNamespaceId { get; set; }

    [Option("property", HelpText = "--property <n1>=<v1>;<n2>=<v2> An optional set of MSBuild properties used when interpreting project files. These are the same properties that are passed to msbuild via the /property:<n1>=<v1>;<n2>=<v2> command line argument")]
    public string MSBuildProperties { get; set; }

    [Option("disableGitFeatures", HelpText = "Disable fetching Git related information for articles. By default it is enabled and may have side effect on performance when the repo is large.")]
    public bool DisableGitFeatures { get; set; }

    [Option("disableDefaultFilter", HelpText = "Disable the default API filter (default filter only generate public or protected APIs).")]
    public bool DisableDefaultFilter { get; set; }

    [Option("namespaceLayout", HelpText = "Determines the namespace layout in table of contents.")]
    public NamespaceLayout? NamespaceLayout { get; set; }
}
