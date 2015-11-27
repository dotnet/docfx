// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;
    using EntityModel;
    using System.Collections.Generic;

    internal class Options : CascadableOptions
    {
        public CommandType? CurrentSubCommand { get; set; }

        [ValueOption(0)]
        public string ConfigFile { get; set; }

        [VerbOption("export", HelpText = "Generate API yaml from project for external reference")]
        public ExportCommandOptions ExportCommand { get; set; } = new ExportCommandOptions();

        [VerbOption("pack", HelpText = "Pack existing API YAML for external reference")]
        public PackCommandOptions PackCommand { get; set; } = new PackCommandOptions();

        [VerbOption("metadata", HelpText = "Generate API YAML metadata")]
        public MetadataCommandOptions MetadataCommand { get; set; } = new MetadataCommandOptions();

        [VerbOption("build", HelpText = "Build the project into documentation")]
        public BuildCommandOptions BuildCommand { get; set; } = new BuildCommandOptions();

        [VerbOption("help", HelpText = "Read the detailed help documentation")]
        public HelpCommandOptions HelpCommand { get; set; } = new HelpCommandOptions();

        [VerbOption("init", HelpText = "Init docfx.json with recommended settings")]
        public InitCommandOptions InitCommand { get; set; } = new InitCommandOptions();

        [VerbOption("serve", HelpText = "Serve a static website")]
        public ServeCommandOptions ServeCommand { get; set; } = new ServeCommandOptions();
    }

    public class CascadableOptions
    {
        [Option('o', "output")]
        public string RootOutputFolder { get; set; }

        [OptionList('t', "template", Separator = ',', HelpText = "Specifies the template name to apply to. If not specified, output YAML file will not be transformed.")]
        public List<string> Templates { get; set; }

        [OptionList("theme", Separator = ',', HelpText = "Specifies which theme to use. By default 'default' theme is offered.")]
        public List<string> Themes { get; set; }

        [Option("raw", HelpText = "Preserve the existing xml comment tags inside 'summary' triple slash comments")]
        public bool PreserveRawInlineComments { get; set; }

        [Option('s', "serve", HelpText = "Host the generated documentation to a website")]
        public bool Serve { get; set; }

        [Option('p', "port", HelpText = "Specify the port of the hosted website")]
        public int? Port { get; set; }

        [Option('f', "force")]
        public bool ForceRebuild { get; set; }

        [Option('l', "log", HelpText = "Specify the file name to save processing log")]
        public string Log { get; set; }

        [Option("logLevel", HelpText = "Specify to which log level will be logged. By default log level >= Info will be logged. The acceptable value could be Verbose, Info, Warning, Error.")]
        public LogLevel? LogLevel { get; set; }
    }
}
