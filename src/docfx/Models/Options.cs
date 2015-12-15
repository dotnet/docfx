// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;

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
}
