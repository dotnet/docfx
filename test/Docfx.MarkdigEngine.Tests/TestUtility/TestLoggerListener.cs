// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.MarkdigEngine.Tests;

internal class TestLoggerListener : ILoggerListener
{
    private readonly Func<ILogItem, bool> _filter;
    public List<ILogItem> Items { get; } = new List<ILogItem>();

    public TestLoggerListener(Func<ILogItem, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _filter = filter;
    }

    public void Dispose()
    {
    }

    public void Flush()
    {
    }

    public void WriteLine(ILogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_filter(item))
        {
            Items.Add(item);
        }
    }

    public static TestLoggerListener CreateLoggerListenerWithPhaseEqualFilter(string phase, LogLevel logLevel = LogLevel.Warning)
    {
        return new TestLoggerListener(iLogItem =>
        {
            if (iLogItem.LogLevel < logLevel)
            {
                return false;
            }

            if (phase == null || (iLogItem?.Phase != null && iLogItem.Phase == phase))
            {
                return true;
            }

            return false;
        });
    }
}
