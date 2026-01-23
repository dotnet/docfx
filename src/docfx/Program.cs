// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Spectre.Console;
using Spectre.Console.Cli;

#nullable enable

namespace Docfx;

internal class Program
{
    private static int _exitCode = (int)ExitCode.Success;

    internal static int Main(string[] args)
    {
        // Register Ctrl+C handler for graceful cancellation
        Console.CancelKeyPress += OnCancelKeyPress;

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

        var result = app.Run(args);

        // If we were cancelled, override the exit code
        if (ExitCodeHelper.IsCancelled)
        {
            return (int)ExitCode.UserCancelled;
        }

        // If an exception occurred that set _exitCode, use that
        if (_exitCode != (int)ExitCode.Success)
        {
            return _exitCode;
        }

        return result;
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        // Prevent the process from terminating immediately
        e.Cancel = true;

        // Set the cancellation flag
        ExitCodeHelper.IsCancelled = true;

        // Log the cancellation
        if (!Console.IsOutputRedirected)
        {
            AnsiConsole.MarkupLine("\n[yellow]Operation cancelled by user.[/]");
        }
        else
        {
            Console.WriteLine("\nOperation cancelled by user.");
        }

        // Cleanup logger
        Logger.Flush();
        Logger.UnregisterAllListeners();
    }

    private static void OnException(Exception e, ITypeResolver? resolver)
    {
        // Try to unwrap AggregateException.
        if (e is AggregateException ae && ae.InnerExceptions.Count == 1)
            e = ae.InnerExceptions[0];

        // Check for cancellation
        if (e is OperationCanceledException)
        {
            ExitCodeHelper.IsCancelled = true;
            _exitCode = (int)ExitCode.UserCancelled;
            return;
        }

        // Set exit code based on exception type
        _exitCode = ExitCodeHelper.GetExitCodeForException(e);

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
                Logger.LogError(ex.ToString(), code: ErrorCodes.Build.FatalError);
        }

        // Cleanup logger.
        Logger.Flush();
        Logger.UnregisterAllListeners();
    }
}
