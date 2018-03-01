// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;

    using CommandLine;

    using Microsoft.DocAsCode.Common;

    [OptionUsage("dependency [<dependency output file path>]")]
    internal class DependencyCommandOptions : ICanPrintHelpMessage
    {
        [ValueOption(0)]
        public string DependencyFile { get; set; }

        [Option("intermediateFolder", HelpText = "The intermediate folder that store cache files")]
        public string IntermediateFolder { get; set; }

        [Option('v', "version", HelpText = "The version name of the content")]
        public string VersionName { get; set; }

        [Option('h', "help", HelpText = "Print help message for this sub-command")]
        public bool PrintHelpMessage { get; set; }
    }
}