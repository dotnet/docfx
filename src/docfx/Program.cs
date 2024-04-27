// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console;
using Spectre.Console.Cli;

#nullable enable

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
            config.SetExceptionHandler(OnException);

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

        static void OnException(Exception e, ITypeResolver? resolver)
        {
            if (e is CommandAppException cae)
            {
                if (cae.Pretty is { } pretty)
                    AnsiConsole.Write(pretty);
                else
                    AnsiConsole.MarkupInterpolated($"[red]Error:[/] {e.Message}");
            }
            else
            {
                AnsiConsole.WriteException(e, new ExceptionSettings()
                {
                    Format = ExceptionFormats.ShortenEverything,
                    Style = new()
                    {
                        ParameterName = Color.Grey,
                        ParameterType = Color.Grey78,
                        LineNumber = Color.Grey78,
                    },
                });
            }
        }
    }
}
