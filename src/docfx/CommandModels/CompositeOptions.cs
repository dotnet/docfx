// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using Microsoft.DocAsCode.EntityModel;
    using CommandLine;

    internal class CompositeOptions
    {
        [ValueOption(0)]
        public string ConfigFile { get; set; }

        [Option("plugin")]
        public string PluginFolder { get; set; }

        [Option('h', "help")]
        public bool IsHelp { get; set; }

        [Option('l', "log")]
        public string Log { get; set; }

        [Option("logLevel")]
        public LogLevel? LogLevel { get; set; }
    }
}
