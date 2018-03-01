// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;

    [OptionUsage("init")]
    internal class InitCommandOptions : ICanPrintHelpMessage
    {
        [Option('h', "help", HelpText = "Print help message for this sub-command")]
        public bool PrintHelpMessage { get; set; }

        [Option('q', "quiet", HelpText = "Quietly generate the default docfx.json")]
        public bool Quiet { get; set; }

        [Option("overwrite", HelpText = "Specify if the current file will be overwritten if it exists")]
        public bool Overwrite { get; set; }

        [Option('o', "output", HelpText = "Specify the output folder of the config file. If not specified, the config file will be saved to a new folder docfx_project")]
        public string OutputFolder { get; set; }

        [Option('f', "file", HelpText = "Generate config file docfx.json only, no project folder will be generated")]
        public bool OnlyConfigFile { get; set; }

        [Option("apiGlobPattern", HelpText = "Specify the source project files' glob pattern to generate metadata")]
        public string ApiSourceGlobPattern { get; set; }

        [Option("apiSourceFolder", HelpText = "Specify the source working folder for source project files to start glob search")]
        public string ApiSourceFolder { get; set; }
    }
}
