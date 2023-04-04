// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.DocAsCode;

[Verb("template", HelpText = "List or export existing template")]
internal class TemplateCommandOptions
{
    [Value(0, MetaName = "command", HelpText = "Subcommand name")]
    public IEnumerable<string> Commands { get; set; }

    [Option('A', "all", HelpText = "If specified, all the available templates will be exported.")]
    public bool All { get; set; }

    [Option('o', "output", HelpText = "Specify the output folder path for the exported templates")]
    public string OutputFolder { get; set; }
}
