// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Cli;

namespace Docfx;

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
            config.SetExceptionHandler(e =>
            {
                if (e is CommandAppException cae && cae.Pretty is { } pretty)
                    AnsiConsole.Write(pretty);
                else
                    AnsiConsole.WriteException(e, ExceptionFormats.ShortenEverything);
            });

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
