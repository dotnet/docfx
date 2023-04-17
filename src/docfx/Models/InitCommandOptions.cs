// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

[Description("Generate an initial docfx.json following the instructions")]
internal class InitCommandOptions : CommandSettings
{
    [Description("Quietly generate the default docfx.json")]
    [CommandOption("-q|--quiet")]
    public bool Quiet { get; set; }

    [Description("Specify if the current file will be overwritten if it exists")]
    [CommandOption("--overwrite")]
    public bool Overwrite { get; set; }

    [Description("Specify the output folder of the config file. If not specified, the config file will be saved to a new folder docfx_project")]
    [CommandOption("-o|--output")]
    public string OutputFolder { get; set; }

    [Description("Generate config file docfx.json only, no project folder will be generated")]
    [CommandOption("-f|--file")]
    public bool OnlyConfigFile { get; set; }
}
