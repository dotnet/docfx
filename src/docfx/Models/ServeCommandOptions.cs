// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;

    [OptionUsage("serve <folder path>")]
    internal class ServeCommandOptions : ICanPrintHelpMessage
    {
        [ValueOption(0)]
        public string Folder { get; set; }

        [Option('n', "hostname", HelpText = "Specify the hostname of the hosted website [localhost]")]
        public string Host { get; set; }

        [Option('p', "port", HelpText = "Specify the port of the hosted website [8080]")]
        public int? Port { get; set; }

        [Option('h', "help", HelpText = "Print help message for this sub-command")]
        public bool PrintHelpMessage { get; set; }
    }
}
