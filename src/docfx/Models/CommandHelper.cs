// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx;

internal class CommandHelper
{
    public static (T, string baseDirectory) GetConfig<T>(string configFile)
    {
        if (string.IsNullOrEmpty(configFile))
            configFile = "docfx.json";

        configFile = Path.GetFullPath(configFile);

        if (!File.Exists(configFile))
            throw new FileNotFoundException($"Cannot find config file {configFile}");

        return (JsonUtility.Deserialize<T>(configFile), Path.GetDirectoryName(configFile));
    }

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

        var buildOption = options as BuildCommandOptions;
        var root = Path.GetDirectoryName(buildOption?.ConfigFile ?? Directory.GetCurrentDirectory());

        if (!string.IsNullOrWhiteSpace(options.LogFilePath))
        {
            Logger.RegisterListener(new ReportLogListener(options.LogFilePath, options.RepoRoot ?? string.Empty, root));
        }

        if (options.LogLevel.HasValue)
        {
            Logger.LogLevelThreshold = options.LogLevel.Value;
        }

        Logger.WarningsAsErrors = options.WarningsAsErrors;

        using var _ = new PerformanceScope(string.Empty, LogLevel.Info);

        run();

        Logger.Flush();
        Logger.UnregisterAllListeners();
        Logger.PrintSummary();

        return Logger.HasError ? -1 : 0;
    }
}
