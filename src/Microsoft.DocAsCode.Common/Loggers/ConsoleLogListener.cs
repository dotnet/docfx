// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Common;

public sealed class ConsoleLogListener : ILoggerListener
{
    private const LogLevel LogLevelThreshold = LogLevel.Verbose;

    public void WriteLine(ILogItem item)
    {
        var level = item.LogLevel;
        if (level < LogLevelThreshold)
            return;

        var consoleColor = GetConsoleColor(level);

        var message = new StringBuilder();
        if (!string.IsNullOrEmpty(item.File))
        {
            message.Append($"{Path.GetFullPath(Path.Combine(EnvironmentContext.BaseDirectory, item.File))}");
            if (!string.IsNullOrEmpty(item.Line))
                message.Append($"({item.Line},1)");
            if (!string.IsNullOrEmpty(item.Code))
                message.Append($": {item.LogLevel.ToString().ToLowerInvariant()} {item.Code}");
            message.Append(": ");
        }

        message.Append(item.Message);

        ConsoleUtility.WriteLine(message.ToString(), consoleColor);
    }

    public void Dispose()
    {
    }

    public void Flush()
    {
    }

    private ConsoleColor GetConsoleColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Verbose => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Suggestion => ConsoleColor.Blue,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => throw new NotSupportedException(level.ToString()),
        };
    }
}
