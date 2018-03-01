// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;

    [OptionUsage("download [<output xref archive>]")]
    internal class DownloadCommandOptions : ICanPrintHelpMessage
    {
        [ValueOption(0)]
        public string ArchiveFile { get; set; }

        [Option('x', "xref", HelpText = "Specify the url of xrefmap.")]
        public string Uri { get; set; }

        [Option('h', "help", HelpText = "Print help message for this sub-command")]
        public bool PrintHelpMessage { get; set; }
    }
}
