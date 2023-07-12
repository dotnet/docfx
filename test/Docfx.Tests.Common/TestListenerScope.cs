// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.Tests.Common;

public class TestListenerScope : IDisposable
{
    private readonly TestLoggerListener _listener;
    private readonly LoggerPhaseScope _scope;

    public TestListenerScope(string phaseName)
    {
        _listener = TestLoggerListener.CreateLoggerListenerWithPhaseStartFilter(phaseName);
        Logger.RegisterListener(_listener);
        _scope = new LoggerPhaseScope(phaseName);
    }

    public void Dispose()
    {
        _scope.Dispose();
        Logger.UnregisterListener(_listener);
        _listener.Dispose();
    }

    public List<ILogItem> Items => _listener.Items;
}
