// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;
using Docfx.Dotnet;
using Newtonsoft.Json;
using Spectre.Console.Cli;

namespace Docfx;

class DefaultCommand : Command<DefaultCommand.Options>
{
    [Description("Runs metadata, build and pdf commands")]
    internal class Options : BuildCommandOptions
    {
        [Description("Prints version information")]
        [CommandOption("-v|--version")]
        public bool Version { get; set; }
    }

    public override int Execute(CommandContext context, Options options)
    {
        if (options.Version)
        {
            Console.WriteLine(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            return 0;
        }

        return CommandHelper.Run(options, () =>
        {
            var (config, baseDirectory) = CommandHelper.GetConfig<Config>(options.ConfigFile);
            var outputFolder = options.OutputFolder;
            string serveDirectory = null;

            if (config.Metadata is not null)
            {
                DotnetApiCatalog.Exec(config.Metadata, new(), baseDirectory).GetAwaiter().GetResult();
            }

            if (config.Build is not null)
            {
                BuildCommand.MergeOptionsToConfig(options, config.Build, baseDirectory);
                serveDirectory = RunBuild.Exec(config.Build, new(), baseDirectory, outputFolder);
            }

            if (config.Pdf is not null)
            {
                BuildCommand.MergeOptionsToConfig(options, config.Pdf, baseDirectory);
                RunPdf.Exec(config.Pdf, new(), baseDirectory, outputFolder);
            }

            if (options.Serve && serveDirectory is not null)
            {
                RunServe.Exec(serveDirectory, options.Host, options.Port, options.OpenBrowser, options.OpenFile);
            }
        });
    }

    class Config
    {
        [JsonProperty("build")]
        [JsonPropertyName("build")]
        public BuildJsonConfig Build { get; set; }

        [JsonProperty("metadata")]
        [JsonPropertyName("metadata")]
        public MetadataJsonConfig Metadata { get; set; }

        [JsonProperty("pdf")]
        [JsonPropertyName("pdf")]
        public PdfJsonConfig Pdf { get; set; }
    }
}
