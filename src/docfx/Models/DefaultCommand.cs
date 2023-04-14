// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Microsoft.DocAsCode.Dotnet;
using Newtonsoft.Json;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

class DefaultCommand : Command<DefaultCommand.Options>
{
    [Description("Runs metadata, build and pdf commands")]
    internal class Options : LogOptions
    {
        [Description("Specify the output base directory")]
        [CommandOption("-o|--output")]
        public string OutputFolder { get; set; }

        [Description("Path to docfx.json")]
        [CommandArgument(0, "[config]")]
        public string Config { get; set; }
    }

    public override int Execute(CommandContext context, Options options)
    {
        return CommandHelper.Run(options, () =>
        {
            var (config, baseDirectory) = CommandHelper.GetConfig<Config>(options.Config);
            var outputFolder = options.OutputFolder;

            if (config.Metadata is not null)
                DotnetApiCatalog.Exec(config.Metadata, new(), baseDirectory, outputFolder).GetAwaiter().GetResult();
            if (config.Build is not null)
                RunBuild.Exec(config.Build, new(), baseDirectory, outputFolder);
            if (config.Pdf is not null)
                RunPdf.Exec(config.Pdf, new(), baseDirectory, outputFolder);
        });
    }

    class Config
    {
        [JsonProperty("build")]
        public BuildJsonConfig Build { get; set; }

        [JsonProperty("metadata")]
        public MetadataJsonConfig Metadata { get; set; }

        [JsonProperty("pdf")]
        public PdfJsonConfig Pdf { get; set; }
    }
}
