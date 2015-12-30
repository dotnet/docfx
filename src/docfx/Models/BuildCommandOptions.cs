// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System.Collections.Generic;

    using CommandLine;
    using Microsoft.DocAsCode.EntityModel;

    internal class BuildCommandOptions : IHasHelp
    {
        [Option('o', "output")]
        public string OutputFolder { get; set; }

        [ValueOption(0)]
        public string ConfigFile { get; set; }

        [Option("help")]
        public bool IsHelp { get; set; }

        [Option('l', "log", HelpText = "Specify the file name to save processing log")]
        public string Log { get; set; }

        [Option("logLevel", HelpText = "Specify to which log level will be logged. By default log level >= Info will be logged. The acceptable value could be Verbose, Info, Warning, Error.")]
        public LogLevel? LogLevel { get; set; }

        [OptionList("content", Separator = ',', HelpText = "Specifies content files for generating documentation.")]
        public List<string> Content { get; set; }

        [OptionList("resource", Separator = ',', HelpText = "Specifies resources used by content files.")]
        public List<string> Resource { get; set; }

        [OptionList("overwrite", Separator = ',', HelpText = "Specifies overwrite files used by content files.")]
        public List<string> Overwrite { get; set; }

        [OptionList("externalReference", Separator = ',', HelpText = "Specifies external reference files used by content files.")]
        public List<string> ExternalReference { get; set; }

        [OptionList('t', "template", Separator = ',', HelpText = "Specifies the template name to apply to. If not specified, output YAML file will not be transformed.")]
        public List<string> Templates { get; set; }

        [OptionList("theme", Separator = ',', HelpText = "Specifies which theme to use. By default 'default' theme is offered.")]
        public List<string> Themes { get; set; }

        [Option('s', "serve", HelpText = "Host the generated documentation to a website")]
        public bool Serve { get; set; }

        [Option('p', "port", HelpText = "Specify the port of the hosted website")]
        public int? Port { get; set; }
    }
}
