// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.MarkdigEngine.Tests;

internal class TestLoggerListener : ILoggerListener
{
    public List<ILogItem> Items { get; } = [];

    public void Dispose()
    {
    }

    public void Flush()
    {
    }

    public void WriteLine(ILogItem item)
    {
        Items.Add(item);
    }
}
