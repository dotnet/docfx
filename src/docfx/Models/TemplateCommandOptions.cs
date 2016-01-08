// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using CommandLine;

    [OptionUsage("template list")]
    [OptionUsage("template export [<name>]")]
    internal class TemplateCommandOptions : ICanPrintHelpMessage
    {
        [ValueList(typeof(List<string>))]
        public List<string> Commands { get; set; }

        [Option('A', "all", HelpText = "If specified, all the available templates will be exported.")]
        public bool All { get; set; }

        [Option('h', "help", HelpText = "Print help message for this sub-command")]
        public bool PrintHelpMessage { get; set; }

        [Option('o', "output", HelpText = "Specify the output folder path for the exported templates")]
        public string OutputFolder { get; set; }
    }
}
