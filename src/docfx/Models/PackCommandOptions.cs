// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;
    using Microsoft.DocAsCode.EntityModel;

    [OptionUsage("pack")]
    internal class PackCommandOptions : IHasHelp, IHasLog
    {
        [Option('u', "url", HelpText = "The base url of yaml file.", Required = true)]
        public string BaseUrl { get; set; }

        [Option('s', "source", HelpText = "The base folder for yaml files.", Required = true)]
        public string Source { get; set; }

        [Option('g', "glob", HelpText = "The glob partten for yaml files.", Required = true)]
        public string Glob { get; set; }

        [Option('n', "name", HelpText = "The name of package.")]
        public string Name { get; set; }

        [Option('a', "append", HelpText = "Append the package.")]
        public bool AppendMode { get; set; }

        [Option('f', "flat", HelpText = "Flat href path.")]
        public bool FlatMode { get; set; }

        [Option('o', "output")]
        public string OutputFolder { get; set; }

        [Option('h', "help", HelpText = "Print help message for this sub-command")]
        public bool IsHelp { get; set; }

        [Option('l', "log", HelpText = "Specify the file name to save processing log")]
        public string Log { get; set; }

        [Option("logLevel", HelpText = "Specify to which log level will be logged. By default log level >= Info will be logged. The acceptable value could be Verbose, Info, Warning, Error.")]
        public LogLevel? LogLevel { get; set; }
    }
}
