// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Exceptions;
using Microsoft.DocAsCode.Plugins;
using Microsoft.DocAsCode.SubCommands;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

internal class Program
{
    internal static int Main(string[] args)
    {
        EnvironmentContext.SetVersion(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<InitCommand>("init");
            config.AddCommand<BuildCommand>("build");
            config.AddCommand<MetadataCommand>("metadata");
            config.AddCommand<ServeCommand>("serve");
            config.AddCommand<PdfCommand>("pdf");
            config.AddCommand<TemplateCommand>("template");
            config.AddCommand<DownloadCommand>("download");
            config.AddCommand<MergeCommand>("merge");
        });

        return app.Run(args);
    }

    private static Func<T, int> Run<T>(Action<T> run, bool showSummary = false)
    {
        return RunCore;

        int RunCore(T options)
        {
            var consoleLogListener = new ConsoleLogListener();
            Logger.RegisterListener(consoleLogListener);

            PerformanceScope scope = null;
            try
            {
                // TODO: For now reuse AllowReplay for overall elapsed time statistics
                if (showSummary)
                {
                    scope = new PerformanceScope(string.Empty, LogLevel.Info);
                }

                run(options);

                Logger.Flush();
                Logger.UnregisterAllListeners();

                if (showSummary)
                {
                    Logger.PrintSummary();
                }

                return Logger.HasError ? -1 : 0;
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.Flatten().InnerExceptions)
                {
                    LogExceptionError(e);
                }
                return 1;
            }
            catch (Exception e)
            {
                LogExceptionError(e);
                return 1;
            }
            finally
            {
                scope?.Dispose();
            }
        }
    }

    private static void LogExceptionError(Exception exception)
    {
        if (exception is DocumentException)
        {
            return;
        }
        else if (exception is DocfxException docfxException)
        {
            Logger.LogError(docfxException.Message, code: ErrorCodes.Build.FatalError);
            return;
        }
        else
        {
            Logger.LogError(exception.ToString(), code: ErrorCodes.Build.FatalError);
        }
    }
}
