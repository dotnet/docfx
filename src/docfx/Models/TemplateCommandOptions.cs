// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

[Description("List or export existing template")]
internal class TemplateCommandOptions : CommandSettings
{
    [Description("Subcommand name")]
    [CommandArgument(0, "command")]
    public IEnumerable<string> Commands { get; set; }

    [Description("If specified, all the available templates will be exported.")]
    [CommandOption("-A|--all")]
    public bool All { get; set; }

    [Description("Specify the output folder path for the exported templates")]
    [CommandOption("-o|--output")]
    public string OutputFolder { get; set; }
}
