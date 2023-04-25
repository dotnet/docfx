// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

internal class Program
{
    internal static int Main(string[] args)
    {
        var app = new CommandApp();

        app.SetDefaultCommand<DefaultCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("docfx");
            config.UseStrictParsing();

            config.AddCommand<InitCommand>("init");
            config.AddCommand<BuildCommand>("build");
            config.AddCommand<MetadataCommand>("metadata");
            config.AddCommand<ServeCommand>("serve");
            config.AddCommand<PdfCommand>("pdf");
            config.AddBranch("template", template =>
            {
                template.AddCommand<TemplateCommand.ListCommand>("list");
                template.AddCommand<TemplateCommand.ExportCommand>("export");
            });
            config.AddCommand<DownloadCommand>("download");
            config.AddCommand<MergeCommand>("merge");
        });

        return app.Run(args);
    }
}
