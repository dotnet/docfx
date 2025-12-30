// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
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
            // Try to unwrap AggregateException.
            if (e is AggregateException ae && ae.InnerExceptions.Count == 1)
                e = ae.InnerExceptions[0];

            if (!Console.IsOutputRedirected)
            {
                // Write exception to console.
                AnsiConsole.Console.WriteException(e);

                // Write exception to ReportLogListener if exists.
                var reportLogListener = Logger.FindListener(x => x is ReportLogListener);
                reportLogListener?.WriteLine(Logger.GetLogItem(LogLevel.Error, e.ToString(), code: ErrorCodes.Build.FatalError));
            }
            else
            {
                // Write exception with Logger API if stdout is redirected.
                // To avoid line wrap issue https://github.com/spectreconsole/spectre.console/issues/1782
                var exceptions = e is AggregateException ae2
                    ? ae2.Flatten().InnerExceptions.ToArray()
                    : [e];

                foreach (var ex in exceptions)
                    Logger.LogError(e.ToString(), code: ErrorCodes.Build.FatalError);
            }

            // Cleanup logger.
            Logger.Flush();
            Logger.UnregisterAllListeners();
        }
    }
}
