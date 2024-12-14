// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.Tests.Common;

public class TestLoggerListener : ILoggerListener
{
    public List<ILogItem> Items { get; } = [];

    private readonly Func<ILogItem, bool> _filter;

    public TestLoggerListener(Func<ILogItem, bool> filter = null)
    {
        _filter = filter;
    }

    public void WriteLine(ILogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_filter is null || _filter(item))
        {
            Items.Add(item);
        }
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }

    public static TestLoggerListener CreateLoggerListenerWithCodeFilter(string code, LogLevel logLevel = LogLevel.Warning)
        => new(i => i.LogLevel >= logLevel && i.Code == code);

    public static TestLoggerListener CreateLoggerListenerWithCodesFilter(List<string> codes, LogLevel logLevel = LogLevel.Warning)
        => new(i => i.LogLevel >= logLevel && codes.Contains(i.Code));

    public IEnumerable<ILogItem> GetItemsByLogLevel(LogLevel logLevel)
    {
        return Items.Where(i => i.LogLevel == logLevel);
    }
}
