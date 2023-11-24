// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public sealed class ReportLogListener : ILoggerListener
{
    private readonly StreamWriter _writer;

    public ReportLogListener(string reportPath)
    {
        var dir = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        _writer = new StreamWriter(reportPath, true);
    }

    public void WriteLine(ILogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _writer.WriteLine(JsonUtility.Serialize(new
        {
            severity = item.LogLevel,
            message = item.Message,
            file = item.File,
            line = item.Line,
            date_time = DateTime.UtcNow,
            code = item.Code,
        }));
    }

    public void Dispose()
    {
        _writer.Dispose();
    }

    public void Flush()
    {
        _writer.Flush();
    }
}
