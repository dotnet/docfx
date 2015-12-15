// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;

    internal class InitCommandOptions
    {
        [Option('q', "quiet", HelpText = "Quietly generate the default docfx.json")]
        public bool Quiet { get; set; }

        [Option('n', "name", HelpText = "Specify the name of the config file generated", DefaultValue = "docfx.json")]
        public string Name { get; set; }

        [Option('o', "output", HelpText = "Specify the output folder of the config file. If not specified, the config file will be saved to current folder")]
        public string OutputFolder { get; set; }
    }
}
