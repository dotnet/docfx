// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.Tests.Common;

public class TestListenerScope : ILoggerListener, IDisposable
{
    private static AsyncLocal<List<ILogItem>> s_items = new();

    public List<ILogItem> Items => s_items.Value;

    public TestListenerScope()
    {
        s_items.Value = new();
        Logger.RegisterListener(this);
    }

    public void Flush() { }

    public void WriteLine(ILogItem item)
    {
        if (item.LogLevel >= LogLevel.Warning)
            s_items.Value.Add(item);
    }

    public IEnumerable<ILogItem> GetItemsByLogLevel(LogLevel logLevel)
    {
        return Items.Where(i => i.LogLevel == logLevel);
    }

    public void Dispose()
    {
        if (s_items.Value is not null)
        {
            s_items.Value = null;
            Logger.UnregisterListener(this);
        }
    }
}
