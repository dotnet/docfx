// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Common;

public sealed class LoggerPhaseScope : IDisposable
{
    private readonly string _originPhaseName;
    private readonly PerformanceScope _performanceScope;

    public LoggerPhaseScope(string phaseName)
        : this(phaseName, null, null) { }

    public LoggerPhaseScope(string phaseName, LogLevel perfLogLevel)
        : this(phaseName, (LogLevel?)perfLogLevel, null) { }

    public LoggerPhaseScope(string phaseName, LogLevel perfLogLevel, AggregatedPerformanceScope aggregatedPerformanceLogger)
        : this(phaseName, (LogLevel?)perfLogLevel, aggregatedPerformanceLogger) { }

    private LoggerPhaseScope(string phaseName, LogLevel? perfLogLevel, AggregatedPerformanceScope aggregatedPerformanceLogger)
    {
        if (string.IsNullOrWhiteSpace(phaseName))
        {
            throw new ArgumentException("Phase name cannot be null or white space.", nameof(phaseName));
        }

        _originPhaseName = GetPhaseName();
        phaseName = _originPhaseName == null ? phaseName : _originPhaseName + "." + phaseName;
        SetPhaseName(phaseName);
        if (perfLogLevel != null)
        {
            _performanceScope = new PerformanceScope("Scope:" + phaseName, perfLogLevel.Value, aggregatedPerformanceLogger);
        }
    }

    private LoggerPhaseScope(CapturedLoggerPhaseScope captured, LogLevel? perfLogLevel)
    {
        _originPhaseName = GetPhaseName();
        SetPhaseName(captured.PhaseName);
        if (perfLogLevel != null)
        {
            _performanceScope = new PerformanceScope("Scope:" + captured.PhaseName, perfLogLevel.Value);
        }
    }

    public static T WithScope<T>(string phaseName, Func<T> func)
    {
        using (new LoggerPhaseScope(phaseName))
        {
            return func();
        }
    }

    public static T WithScope<T>(string phaseName, LogLevel perfLogLevel, Func<T> func)
    {
        using (new LoggerPhaseScope(phaseName, perfLogLevel))
        {
            return func();
        }
    }

    public void Dispose()
    {
        _performanceScope?.Dispose();
        SetPhaseName(_originPhaseName);
    }

    internal static string GetPhaseName()
    {
        return LogicalCallContext.GetData(nameof(LoggerPhaseScope)) as string;
    }

    private void SetPhaseName(string phaseName)
    {
        LogicalCallContext.SetData(nameof(LoggerPhaseScope), phaseName);
    }

    public static object Capture()
    {
        return new CapturedLoggerPhaseScope();
    }

    public static LoggerPhaseScope Restore(object captured) =>
        Restore(captured, null);

    public static LoggerPhaseScope Restore(object captured, LogLevel perfLogLevel) =>
        Restore(captured, (LogLevel?)perfLogLevel);

    private static LoggerPhaseScope Restore(object captured, LogLevel? perfLogLevel)
    {
        if (!(captured is CapturedLoggerPhaseScope capturedScope))
        {
            return null;
        }
        return new LoggerPhaseScope(capturedScope, perfLogLevel);
    }

    private sealed class CapturedLoggerPhaseScope
    {
        public CapturedLoggerPhaseScope()
        {
            PhaseName = GetPhaseName();
        }

        public string PhaseName { get; }
    }
}
