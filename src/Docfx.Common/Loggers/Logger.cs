// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Spectre.Console;

namespace Docfx.Common;

public static class Logger
{
    public const int WarningThrottling = 10000;
    public static bool HasError { get; private set; }
    public static int WarningCount => _warningCount;
    public static int ErrorCount => _errorCount;

    private static readonly CompositeLogListener _syncListener = new();
    private static int _warningCount = 0;
    private static int _errorCount = 0;
    public static volatile LogLevel LogLevelThreshold = LogLevel.Info;
    public static volatile bool WarningsAsErrors = false;
    public static volatile Dictionary<string, LogLevel> Rules;

    public static void RegisterListener(ILoggerListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        _syncListener.AddListener(listener);
    }

    public static void RegisterListeners(IEnumerable<ILoggerListener> listeners)
    {
        ArgumentNullException.ThrowIfNull(listeners);

        _syncListener.AddListeners(listeners);
    }

    public static IEnumerable<ILoggerListener> GetAllListeners()
    {
        return _syncListener.GetAllListeners();
    }

    public static ILoggerListener FindListener(Predicate<ILoggerListener> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _syncListener.FindListener(predicate);
    }

    public static void UnregisterListener(ILoggerListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        _syncListener.RemoveListener(listener);
    }

    public static void UnregisterAllListeners()
    {
        _syncListener.RemoveAllListeners();
    }

    private static void Log(LogItem item)
    {
        if (Rules is not null && !string.IsNullOrEmpty(item.Code) && Rules.TryGetValue(item.Code, out var level))
        {
            item.LogLevel = level;
        }

        if (item.LogLevel < LogLevelThreshold)
        {
            return;
        }

        if (item.LogLevel == LogLevel.Warning)
        {
            if (WarningsAsErrors)
            {
                HasError = true;
            }

            var count = Interlocked.Increment(ref _warningCount);
            if (count > WarningThrottling)
            {
                return;
            }
            else if (count == WarningThrottling)
            {
                var msg = new LogItem
                {
                    Code = WarningCodes.Build.TooManyWarnings,
                    LogLevel = LogLevel.Warning,
                    Message = "Too many warnings, no more warning will be logged."
                };
                _syncListener.WriteLine(msg);
            }
        }

        if (item.LogLevel == LogLevel.Error)
        {
            HasError = true;
            Interlocked.Increment(ref _errorCount);
        }

        Debug.WriteLine(item.Message);
        _syncListener.WriteLine(item);
    }

    public static void Log(LogLevel level, string message, string phase = null, string file = null, string line = null, string code = null)
    {
        Log(new LogItem
        {
            File = file ?? LoggerFileScope.GetFileName(),
            Line = line,
            LogLevel = level,
            Message = message,
            Code = code,
        });
    }

    public static ILogItem GetLogItem(LogLevel level, string message, string phase = null, string file = null, string line = null, string code = null)
    {
        return new LogItem
        {
            File = file ?? LoggerFileScope.GetFileName(),
            Line = line,
            LogLevel = level,
            Message = message,
            Code = code,
        };
    }

    public static void LogDiagnostic(string message, string phase = null, string file = null, string line = null, string code = null)
    {
        Log(LogLevel.Diagnostic, message, phase, file, line, code);
    }

    public static void LogVerbose(string message, string phase = null, string file = null, string line = null, string code = null)
    {
        Log(LogLevel.Verbose, message, phase, file, line, code);
    }

    public static void LogInfo(string message, string phase = null, string file = null, string line = null, string code = null)
    {
        Log(LogLevel.Info, message, phase, file, line, code);
    }

    public static void LogSuggestion(string message, string phase = null, string file = null, string line = null, string code = null)
    {
        Log(LogLevel.Suggestion, message, phase, file, line, code);
    }

    public static void LogWarning(string message, string phase = null, string file = null, string line = null, string code = null)
    {
        Log(LogLevel.Warning, message, phase, file, line, code);
    }

    public static void LogError(string message, string phase = null, string file = null, string line = null, string code = null)
    {
        Log(LogLevel.Error, message, phase, file, line, code);
    }

    public static void Log(object result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Log(LogLevel.Info, result.ToString());
    }

    public static void Flush()
    {
        _syncListener.Flush();
    }

    public static void PrintSummary()
    {
        if (_errorCount > 0)
            AnsiConsole.MarkupLine("\n\n[red]Build failed.[/]\n");
        else if (_warningCount > 0)
            AnsiConsole.MarkupLine("\n\n[yellow]Build succeeded with warning.[/]\n");
        else
            AnsiConsole.MarkupLine("\n\n[green]Build succeeded.[/]\n");

        AnsiConsole.Foreground = _warningCount > 0 ? ConsoleColor.Yellow : ConsoleColor.White;
        AnsiConsole.WriteLine($"    {_warningCount} warning(s)");
        AnsiConsole.Foreground = _errorCount > 0 ? ConsoleColor.Red : ConsoleColor.White;
        AnsiConsole.WriteLine($"    {_errorCount} error(s)\n");
    }

    public static void ResetCount()
    {
        _warningCount = 0;
        _errorCount = 0;
        HasError = false;
    }

    class LogItem : ILogItem
    {
        public string File { get; set; }

        public string Line { get; set; }

        public LogLevel LogLevel { get; set; }

        public string Message { get; set; }

        public string Code { get; set; }
    }
}
