// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx;

internal class CommandHelper
{
    public static int Run(Action run)
    {
        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

        run();

        Logger.Flush();
        Logger.UnregisterAllListeners();

        return 0;
    }

    public static int Run(LogOptions options, Action run)
    {
        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

        if (!string.IsNullOrWhiteSpace(options.LogFilePath))
        {
            Logger.RegisterListener(new ReportLogListener(options.LogFilePath));
        }

        if (options.LogLevel.HasValue)
        {
            Logger.LogLevelThreshold = options.LogLevel.Value;
        }
        else if (options.Verbose)
        {
            Logger.LogLevelThreshold = LogLevel.Verbose;
        }

        Logger.WarningsAsErrors = options.WarningsAsErrors;

        run();

        Logger.Flush();
        Logger.UnregisterAllListeners();
        Logger.PrintSummary();

        return Logger.HasError ? -1 : 0;
    }
}
