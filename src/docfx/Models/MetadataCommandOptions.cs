// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using CommandLine;

    using Microsoft.DocAsCode.Common;

    [OptionUsage("metadata [<docfx.json file path>]")]
    [OptionUsage("metadata <code project1> [<code project2>] ... [<code projectN>]")]
    internal class MetadataCommandOptions : ICanPrintHelpMessage, ILoggable
    {
        [Option('f', "force", HelpText = "Force re-generate all the metadata")]
        public bool ForceRebuild { get; set; }

        [Option("shouldSkipMarkup", HelpText = "Skip to markup the triple slash comments")]
        public bool ShouldSkipMarkup { get; set; }

        public string OutputFolder { get; set; }

        [Option("raw", HelpText = "Preserve the existing xml comment tags inside 'summary' triple slash comments")]
        public bool PreserveRawInlineComments { get; set; }

        [Option('h', "help", HelpText = "Print help message for this sub-command")]
        public bool PrintHelpMessage { get; set; }

        [Option('l', "log", HelpText = "Specify the file name to save processing log")]
        public string LogFilePath { get; set; }

        [Option("logLevel", HelpText = "Specify to which log level will be logged. By default log level >= Info will be logged. The acceptable value could be Verbose, Info, Warning, Error.")]
        public LogLevel? LogLevel { get; set; }

        [Option("repositoryRoot", HelpText = "Specify the GIT repository root folder.")]
        public string RepoRoot { get; set; }

        [ValueList(typeof(List<string>))]
        public List<string> Projects { get; set; }

        [Option("filter", HelpText = "Specify the filter config file")]
        public string FilterConfigFile { get; set; }

        [Option("globalNamespaceId", HelpText = "Specify the name to use for the global namespace")]
        public string GlobalNamespaceId { get; set; }
    }
}
