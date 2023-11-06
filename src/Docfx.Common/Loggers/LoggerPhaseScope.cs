// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public sealed class LoggerPhaseScope : IDisposable
{
    private readonly string _originPhaseName;

    public LoggerPhaseScope(string phaseName)
    {
        if (string.IsNullOrWhiteSpace(phaseName))
        {
            throw new ArgumentException("Phase name cannot be null or white space.", nameof(phaseName));
        }

        _originPhaseName = GetPhaseName();
        phaseName = _originPhaseName == null ? phaseName : _originPhaseName + "." + phaseName;
        SetPhaseName(phaseName);
    }

    public static T WithScope<T>(string phaseName, Func<T> func)
    {
        using (new LoggerPhaseScope(phaseName))
        {
            return func();
        }
    }

    public void Dispose()
    {
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
}
